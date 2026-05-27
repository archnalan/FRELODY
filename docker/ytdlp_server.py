"""
Minimal yt-dlp HTTP sidecar — runs alongside gunicorn on port 8081.
Accepts a YouTube video ID, downloads best audio to /tmp, returns the file path.
frelody-api then passes that path as audio_path to the main chordmini endpoints.

Bot-wall strategy (no end-user auth, no cookies by default):
  YouTube increasingly returns "Sign in to confirm you're not a bot" unless the
  request carries a Proof-of-Origin (PO) token — required now even from many
  residential IPs and almost always from datacenter IPs. We satisfy that with a
  bgutil PO-token provider (a sibling container) instead of a logged-in cookie.
  A Netscape cookies.txt remains an OPTIONAL one-line escalation via the
  YTDLP_COOKIES_FILE env var; it is neither set nor required by default.

Tunables (all via env, so the bypass can be adjusted without code changes):
  YTDLP_PLAYER_CLIENT  yt-dlp youtube player_client list   (default: "default,tv")
  YTDLP_POT_BASE_URL   bgutil HTTP provider base url        (default: http://bgutil-provider:4416)
  YTDLP_COOKIES_FILE   optional Netscape cookies.txt path   (default: unset)
"""

import os
import sys
import json
import subprocess
import uuid
from http.server import BaseHTTPRequestHandler, HTTPServer


# The prebuilt image's bundled yt-dlp (/opt/venv) is too old for the bgutil PO
# framework and its venv is read-only to us. Instead we install a current yt-dlp
# + the plugin into a writable dir on PYTHONPATH (see docker-compose command) and
# invoke it as a module so the newer copy shadows the bundled one.
YTDLP_CMD = [sys.executable, "-m", "yt_dlp"]
PORT = 8081

PLAYER_CLIENT = os.environ.get("YTDLP_PLAYER_CLIENT", "default,tv").strip()
POT_BASE_URL = os.environ.get("YTDLP_POT_BASE_URL", "http://bgutil-provider:4416").strip()
COOKIES_FILE = os.environ.get("YTDLP_COOKIES_FILE", "").strip()


def _ytdlp_version() -> str:
    try:
        out = subprocess.run(YTDLP_CMD + ["--version"], capture_output=True, text=True, timeout=20)
        return out.stdout.strip() or "unknown"
    except Exception as exc:  # diagnostic only
        return f"error: {exc}"


def _extractor_args() -> list:
    """Build the --extractor-args flags shared by every download."""
    args = ["--extractor-args", f"youtube:player_client={PLAYER_CLIENT}"]
    if POT_BASE_URL:
        # bgutil HTTP PO-token provider plugin reads its server URL from here.
        args += ["--extractor-args", f"youtubepot-bgutilhttp:base_url={POT_BASE_URL}"]
    return args


def _resolve_url(body: dict):
    """Accept either a full media URL (TikTok, etc.) or a YouTube videoId."""
    url = (body.get("url") or "").strip()
    if url:
        if not (url.startswith("https://") or url.startswith("http://")):
            return None, "url must be an absolute http(s) URL"
        return url, None
    video_id = (body.get("videoId") or "").strip()
    if video_id:
        return f"https://www.youtube.com/watch?v={video_id}", None
    return None, "videoId or url is required"


def _friendly_error(stderr: str) -> str:
    """Map yt-dlp stderr to a concise, user-friendly message."""
    s = stderr.lower()
    if "sign in" in s or "not a bot" in s or "confirm your age" in s:
        return (
            "YouTube temporarily blocked automated access to this video. "
            "Please try again in a moment, or pick a different video."
        )
    if "private video" in s:
        return "This video is private and cannot be accessed."
    if "video unavailable" in s or "has been removed" in s:
        return "This video is unavailable or has been removed."
    if "age" in s and ("restrict" in s or "limit" in s):
        return "This video is age-restricted and cannot be analyzed."
    if "copyright" in s or "not available in your country" in s or "blocked" in s:
        return "This video is restricted due to copyright or regional limitations."
    if "no video formats" in s or "requested format" in s:
        return "No downloadable audio format is available for this video."
    return "Could not extract audio from this video. Please try again later."


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        pass  # suppress default access log to keep container logs clean

    def _send(self, code: int, body: dict):
        data = json.dumps(body).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path != "/api/ytdlp/health":
            self._send(404, {"error": "Not found"})
            return
        self._send(200, {
            "ok": True,
            "ytdlpVersion": _ytdlp_version(),
            "playerClient": PLAYER_CLIENT,
            "potProvider": POT_BASE_URL or None,
            "cookies": bool(COOKIES_FILE and os.path.isfile(COOKIES_FILE)),
        })

    def _read_body(self) -> dict:
        length = int(self.headers.get("Content-Length", 0))
        return json.loads(self.rfile.read(length)) if length else {}

    def do_POST(self):
        if self.path == "/api/ytdlp/extract":
            self._handle_extract()
        elif self.path == "/api/ytdlp/info":
            self._handle_info()
        else:
            self._send(404, {"error": "Not found"})

    def _handle_extract(self):
        body = self._read_body()
        url, err = _resolve_url(body)
        if err:
            self._send(400, {"error": err})
            return

        uid = uuid.uuid4().hex[:8]
        output_template = f"/tmp/yt_{uid}.%(ext)s"

        cmd = [
            *YTDLP_CMD,
            "-x",
            "--audio-format", "mp3",
            "--audio-quality", "0",
            "-o", output_template,
            "--no-playlist",
            "--quiet",
            "--no-warnings",
        ]
        cmd += _extractor_args()
        if COOKIES_FILE and os.path.isfile(COOKIES_FILE):
            cmd += ["--cookies", COOKIES_FILE]
        cmd.append(url)

        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)

        if result.returncode != 0:
            raw_err = result.stderr.strip() or "yt-dlp failed"
            # Surface raw stderr to container logs for diagnosis; only the
            # friendly message reaches the user.
            print(f"[ytdlp] extract failed for {url}: {raw_err}", file=sys.stderr, flush=True)
            self._send(422, {"error": _friendly_error(raw_err)})
            return

        # Locate the output file
        out_path = f"/tmp/yt_{uid}.mp3"
        if not os.path.exists(out_path):
            # yt-dlp may have used a different extension
            for ext in ("webm", "m4a", "opus", "ogg", "mp4"):
                candidate = f"/tmp/yt_{uid}.{ext}"
                if os.path.exists(candidate):
                    out_path = candidate
                    break
            else:
                self._send(502, {"error": "Output file not found after download"})
                return

        self._send(200, {"filePath": out_path})

    def _handle_info(self):
        """Return lightweight metadata (no download) for any supported URL."""
        body = self._read_body()
        url, err = _resolve_url(body)
        if err:
            self._send(400, {"error": err})
            return

        cmd = [*YTDLP_CMD, "-J", "--skip-download", "--no-playlist", "--no-warnings"]
        cmd += _extractor_args()
        if COOKIES_FILE and os.path.isfile(COOKIES_FILE):
            cmd += ["--cookies", COOKIES_FILE]
        cmd.append(url)

        result = subprocess.run(cmd, capture_output=True, text=True, timeout=120)
        if result.returncode != 0:
            raw_err = result.stderr.strip() or "yt-dlp failed"
            print(f"[ytdlp] info failed for {url}: {raw_err}", file=sys.stderr, flush=True)
            self._send(422, {"error": _friendly_error(raw_err)})
            return

        try:
            info = json.loads(result.stdout)
        except Exception:
            self._send(502, {"error": "Could not read video metadata."})
            return

        thumb = info.get("thumbnail")
        if not thumb:
            thumbs = info.get("thumbnails") or []
            thumb = thumbs[-1].get("url") if thumbs else None

        self._send(200, {
            "id": str(info.get("id") or ""),
            "title": info.get("title") or info.get("description") or "Untitled",
            "uploader": info.get("uploader") or info.get("channel") or info.get("creator"),
            "thumbnail": thumb,
            "durationSeconds": int(info.get("duration") or 0),
            "webpageUrl": info.get("webpage_url") or url,
        })

    def do_DELETE(self):
        if self.path != "/api/ytdlp/cleanup":
            self._send(404, {"error": "Not found"})
            return

        length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(length)) if length else {}
        file_path = body.get("filePath", "")

        if file_path and file_path.startswith("/tmp/yt_") and os.path.exists(file_path):
            os.remove(file_path)

        self._send(200, {"ok": True})


if __name__ == "__main__":
    print(
        f"ytdlp-server listening on port {PORT} | yt-dlp {_ytdlp_version()} | "
        f"player_client={PLAYER_CLIENT} | pot={POT_BASE_URL or 'off'} | "
        f"cookies={'on' if (COOKIES_FILE and os.path.isfile(COOKIES_FILE)) else 'off'}",
        flush=True,
    )
    server = HTTPServer(("0.0.0.0", PORT), Handler)
    server.serve_forever()

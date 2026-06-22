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

Escalation ladder against the datacenter-IP bot-wall (least→most invasive):
  1. PO token (bgutil)        — necessary but NOT sufficient on a datacenter IP
                                (verified live 2026-06-11: ~1/4 videos succeed alone).
  2. Logged-in cookies        — YTDLP_COOKIES_FILE; the #1 fix, fixes most videos.
  3. Residential/mobile proxy — YTDLP_PROXY; the only lever that repairs IP reputation
                                without an account. Robust for scale; combine with (1).
Cookies and proxy are independent and stack — set either, both, or neither.

Tunables (all via env, so the bypass can be adjusted without code changes):
  YTDLP_PLAYER_CLIENT  yt-dlp youtube player_client list   (default: "default,tv")
  YTDLP_POT_BASE_URL   bgutil HTTP provider base url        (default: http://bgutil-provider:4416)
  YTDLP_COOKIES_FILE   optional Netscape cookies.txt path   (default: unset)
  YTDLP_PROXY          optional HTTP/HTTPS/SOCKS proxy URL  (default: unset)
                       e.g. http://user:pass@host:port or socks5://host:port
"""

import os
import sys
import json
import shutil
import subprocess
import tempfile
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


# The prebuilt image's bundled yt-dlp (/opt/venv) is too old for the bgutil PO
# framework and its venv is read-only to us. Instead we install a current yt-dlp
# + the plugin into a writable dir on PYTHONPATH (see docker-compose command) and
# invoke it as a module so the newer copy shadows the bundled one.
YTDLP_CMD = [sys.executable, "-m", "yt_dlp"]
PORT = 8081

PLAYER_CLIENT = os.environ.get("YTDLP_PLAYER_CLIENT", "default,tv").strip()
POT_BASE_URL = os.environ.get("YTDLP_POT_BASE_URL", "http://bgutil-provider:4416").strip()
COOKIES_FILE = os.environ.get("YTDLP_COOKIES_FILE", "").strip()
PROXY = os.environ.get("YTDLP_PROXY", "").strip()
# How many times to attempt a yt-dlp call before giving up on a *transient*
# bot-wall. With the WARP egress a single attempt already succeeds ~always, but
# WARP exit IPs are shared so a video occasionally trips the wall; retrying lifts
# the aggregate success rate (each attempt is a fresh process/connection).
RETRIES = max(1, int(os.environ.get("YTDLP_RETRIES", "3")))


def _proxy_args() -> list:
    """Route every yt-dlp request through a proxy when YTDLP_PROXY is set.

    The active egress is the free Cloudflare WARP sidecar (socks5h://warp:1080) —
    a clean, non-datacenter IP that repairs the datacenter IP's reputation. It
    stacks with the PO token and cookies. Empty/unset = direct connection.
    """
    return ["--proxy", PROXY] if PROXY else []


def _is_transient_wall(stderr: str) -> bool:
    """True if stderr looks like a retryable bot-wall / transient failure.

    These are IP/rate-reputation symptoms that a retry (often via a different
    shared WARP exit IP) can clear. Hard failures (private/removed/age-restricted)
    are deliberately excluded so they fail fast.
    """
    s = (stderr or "").lower()
    return any(t in s for t in (
        "sign in", "not a bot", "requested format is not available",
        "unable to download", "failed to extract", "http error 403",
        "read timed out", "unable to connect",
    ))


def _run_ytdlp(cmd: list, timeout: int) -> subprocess.CompletedProcess:
    """Run yt-dlp, retrying only on transient bot-wall/format failures.

    Bot-wall failures return in seconds, so the retries don't stack toward the
    download timeout in practice; a genuine long download succeeds on attempt 1.
    """
    result = None
    for attempt in range(1, RETRIES + 1):
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
        if result.returncode == 0 or not _is_transient_wall(result.stderr):
            return result
        if attempt < RETRIES:
            print(f"[ytdlp] attempt {attempt}/{RETRIES} hit a transient wall; retrying",
                  file=sys.stderr, flush=True)
            time.sleep(2 * attempt)
    return result


def _ytdlp_version() -> str:
    try:
        out = subprocess.run(YTDLP_CMD + ["--version"], capture_output=True, text=True, timeout=20)
        return out.stdout.strip() or "unknown"
    except Exception as exc:  # diagnostic only
        return f"error: {exc}"


def _extractor_args() -> list:
    """Build the --extractor-args flags shared by every download."""
    # NOTE: do NOT add player_skip=webpage,configs here. ChordMiniApp uses it with
    # android/ios clients, but with the tv_embedded/android_vr client that actually
    # downloads from our datacenter IP it skips the player config needed to resolve
    # format URLs, so a cold-cache container fails every extraction with "Requested
    # format is not available". Verified live 2026-06-22.
    args = ["--extractor-args", f"youtube:player_client={PLAYER_CLIENT}"]
    if POT_BASE_URL:
        # bgutil HTTP PO-token provider plugin reads its server URL from here.
        args += ["--extractor-args", f"youtubepot-bgutilhttp:base_url={POT_BASE_URL}"]
    return args


def _cookie_args() -> tuple:
    """Return (['--cookies', path], cleanup_path) or ([], None).

    yt-dlp rewrites the cookie jar on exit, and the source is bind-mounted
    read-only and shared by both gunicorn workers — writing/sharing it directly
    would error or corrupt it. So hand each call a private throwaway copy and let
    the caller delete it afterwards. The mounted original stays pristine.
    """
    if not (COOKIES_FILE and os.path.isfile(COOKIES_FILE)):
        return [], None
    fd, tmp = tempfile.mkstemp(prefix="ytcookies_", suffix=".txt")
    os.close(fd)
    shutil.copyfile(COOKIES_FILE, tmp)
    return ["--cookies", tmp], tmp


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
    # The sidecar's port is reachable by internet scanners (Censys/Scaleway/AWS
    # were observed opening half-open connections to it). BaseHTTPRequestHandler
    # blocks on rfile.readline() with no socket timeout by default, so a single
    # connection that opens but never sends a request would wedge its worker
    # forever. Capping the per-request socket timeout makes such connections drop
    # instead of hanging. Real downloads use their own subprocess timeout, so this
    # only bounds the time spent reading the (tiny) request line + headers.
    timeout = 30

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
            "proxy": bool(PROXY),
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
        cmd += _proxy_args()
        cookie_flags, cookie_tmp = _cookie_args()
        cmd += cookie_flags
        cmd.append(url)

        try:
            result = _run_ytdlp(cmd, 300)
        finally:
            if cookie_tmp and os.path.exists(cookie_tmp):
                os.remove(cookie_tmp)

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
        cmd += _proxy_args()
        cookie_flags, cookie_tmp = _cookie_args()
        cmd += cookie_flags
        cmd.append(url)

        try:
            result = _run_ytdlp(cmd, 120)
        finally:
            if cookie_tmp and os.path.exists(cookie_tmp):
                os.remove(cookie_tmp)
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

        # Native pixel dimensions so the player stage can match the real aspect
        # ratio (TikTok clips vary between portrait 9:16 and landscape 16:9).
        # yt-dlp exposes top-level width/height; fall back to the best thumbnail.
        width = info.get("width")
        height = info.get("height")
        if not (width and height):
            thumbs = info.get("thumbnails") or []
            best = max(
                (t for t in thumbs if t.get("width") and t.get("height")),
                key=lambda t: t.get("width") * t.get("height"),
                default=None,
            )
            if best:
                width = best.get("width")
                height = best.get("height")

        self._send(200, {
            "id": str(info.get("id") or ""),
            "title": info.get("title") or info.get("description") or "Untitled",
            "uploader": info.get("uploader") or info.get("channel") or info.get("creator"),
            "thumbnail": thumb,
            "durationSeconds": int(info.get("duration") or 0),
            "webpageUrl": info.get("webpage_url") or url,
            "width": int(width) if width else None,
            "height": int(height) if height else None,
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
        f"cookies={'on' if (COOKIES_FILE and os.path.isfile(COOKIES_FILE)) else 'off'} | "
        f"proxy={'on' if PROXY else 'off'}",
        flush=True,
    )
    # ThreadingHTTPServer (not the single-threaded HTTPServer): each connection is
    # handled on its own thread, so one slow/half-open client — or one long
    # extraction (downloads run up to 300s) — can no longer block health checks or
    # other concurrent extraction requests. daemon_threads ensures worker threads
    # don't keep the process alive on shutdown.
    server = ThreadingHTTPServer(("0.0.0.0", PORT), Handler)
    server.daemon_threads = True
    server.serve_forever()

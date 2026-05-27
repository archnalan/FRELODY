"""
Minimal yt-dlp HTTP sidecar — runs alongside gunicorn on port 8081.
Accepts a YouTube video ID, downloads best audio to /tmp, returns the file path.
frelody-api then passes that path as audio_path to the main chordmini endpoints.
"""

import os
import json
import subprocess
import uuid
from http.server import BaseHTTPRequestHandler, HTTPServer


YTDLP = "/opt/venv/bin/yt-dlp"
PORT = 8081

# Optional: mount a Netscape-format cookies.txt and set this env var in docker-compose
COOKIES_FILE = os.environ.get("YTDLP_COOKIES_FILE", "")


def _friendly_error(stderr: str) -> str:
    """Map yt-dlp stderr to a concise, user-friendly message."""
    s = stderr.lower()
    if "sign in" in s or "not a bot" in s or "confirm your age" in s:
        return (
            "YouTube is blocking automated access to this video. "
            "Try a different video or try again later."
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

    def do_POST(self):
        if self.path != "/api/ytdlp/extract":
            self._send(404, {"error": "Not found"})
            return

        length = int(self.headers.get("Content-Length", 0))
        body = json.loads(self.rfile.read(length)) if length else {}
        video_id = body.get("videoId", "").strip()
        if not video_id:
            self._send(400, {"error": "videoId is required"})
            return

        uid = uuid.uuid4().hex[:8]
        output_template = f"/tmp/yt_{video_id}_{uid}.%(ext)s"
        url = f"https://www.youtube.com/watch?v={video_id}"

        cmd = [
            YTDLP,
            "-x",
            "--audio-format", "mp3",
            "--audio-quality", "0",
            "-o", output_template,
            "--no-playlist",
            "--quiet",
            "--no-warnings",
            # Avoid deprecated android_sdkless; fall back through available clients
            "--extractor-args", "youtube:player_client=default,-android_sdkless",
        ]
        if COOKIES_FILE and os.path.isfile(COOKIES_FILE):
            cmd += ["--cookies", COOKIES_FILE]
        cmd.append(url)

        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)

        if result.returncode != 0:
            raw_err = result.stderr.strip() or "yt-dlp failed"
            self._send(502, {"error": _friendly_error(raw_err)})
            return

        # Locate the output file
        out_path = f"/tmp/yt_{video_id}_{uid}.mp3"
        if not os.path.exists(out_path):
            # yt-dlp may have used a different extension
            for ext in ("webm", "m4a", "opus", "ogg", "mp4"):
                candidate = f"/tmp/yt_{video_id}_{uid}.{ext}"
                if os.path.exists(candidate):
                    out_path = candidate
                    break
            else:
                self._send(502, {"error": "Output file not found after download"})
                return

        self._send(200, {"filePath": out_path, "videoId": video_id})

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
    server = HTTPServer(("0.0.0.0", PORT), Handler)
    print(f"ytdlp-server listening on port {PORT}", flush=True)
    server.serve_forever()

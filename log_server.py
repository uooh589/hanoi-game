#!/usr/bin/env python3
"""Simple log receiver + update server for Hanoi Game.

Usage:
  python3 log_server.py                # listen on :8080
  python3 log_server.py --port 9090    # custom port
  python3 log_server.py --public       # listen on all interfaces

Endpoints:
  POST /log       - receive game logs (multipart form: file=log.txt)
  GET  /version   - return latest version string
  POST /upload    - receive new version package (multipart: version=X, win=zip, linux=tar.gz)

All logs stored in ./logs/, uploads in ./uploads/
"""

import argparse
import os
import sys
from datetime import datetime
from http.server import HTTPServer, BaseHTTPRequestHandler
from pathlib import Path

ROOT = Path(__file__).parent
LOG_DIR = ROOT / "received_logs"
UPLOAD_DIR = ROOT / "uploads"
VERSION_FILE = ROOT / "current_version.txt"

LOG_DIR.mkdir(exist_ok=True)
UPLOAD_DIR.mkdir(exist_ok=True)

# Write default version if not exists
if not VERSION_FILE.exists():
    VERSION_FILE.write_text("v1.0.0")


class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):
        print(f"[{datetime.now():%H:%M:%S}] {args[0]}")

    def do_GET(self):
        if self.path == "/version":
            self.send_response(200)
            self.send_header("Content-Type", "text/plain")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.end_headers()
            version = VERSION_FILE.read_text().strip()
            self.wfile.write(version.encode())
            return

        if self.path == "/":
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.end_headers()
            files = sorted(LOG_DIR.glob("*.txt"), key=os.path.getmtime, reverse=True)
            html = ["<h2>Hanoi Game Log Server</h2>",
                    f"<p>Version: {VERSION_FILE.read_text().strip()}</p>",
                    f"<p>Logs received: {len(files)}</p>",
                    "<ul>"]
            for f in files[:50]:
                size = os.path.getsize(f)
                mtime = datetime.fromtimestamp(os.path.getmtime(f))
                html.append(f'<li><a href="/log/{f.name}">{f.name}</a> ({size}B, {mtime:%Y-%m-%d %H:%M})</li>')
            html.append("</ul>")
            self.wfile.write("\n".join(html).encode())
            return

        if self.path.startswith("/log/"):
            name = self.path[5:]
            fp = LOG_DIR / name
            if fp.exists() and fp.is_file():
                self.send_response(200)
                self.send_header("Content-Type", "text/plain; charset=utf-8")
                self.end_headers()
                self.wfile.write(fp.read_bytes())
            else:
                self.send_response(404)
                self.end_headers()
            return

        self.send_response(404)
        self.end_headers()

    def do_POST(self):
        content_type = self.headers.get("Content-Type", "")
        content_length = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(content_length)

        if self.path == "/log":
            self._handle_log(body, content_type)
            return

        if self.path == "/upload":
            self._handle_upload(body, content_type)
            return

        self.send_response(404)
        self.end_headers()

    def _handle_log(self, body, content_type):
        # Parse multipart or raw body
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        log_path = LOG_DIR / f"log_{timestamp}.txt"

        if b"Content-Disposition" in body:
            # multipart form
            idx = body.find(b"\r\n\r\n")
            if idx > 0:
                start = idx + 4
                end = body.find(b"------", start)
                if end < 0:
                    end = len(body)
                log_path.write_bytes(body[start:end].strip())
        else:
            log_path.write_bytes(body)

        size = log_path.stat().st_size
        print(f"  -> Received log: {log_path.name} ({size} bytes)")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()
        self.wfile.write(b'{"ok":true}')

    def _handle_upload(self, body, content_type):
        # Parse multipart upload with version + files
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")

        # Extract version from multipart
        version = "unknown"
        parts = body.split(b"------")
        for part in parts:
            if b'name="version"' in part:
                idx = part.find(b"\r\n\r\n")
                if idx > 0:
                    version = part[idx+4:].strip().decode(errors="ignore")
                    VERSION_FILE.write_text(version + "\n")
                    print(f"  -> Version updated to {version}")

            if b'name="win"' in part and b'filename=' in part:
                idx = part.find(b"\r\n\r\n")
                if idx > 0:
                    start = idx + 4
                    end = part.rfind(b"\r\n")
                    data = part[start:end]
                    path = UPLOAD_DIR / f"HanoiGame_{version}_Win64_{timestamp}.zip"
                    path.write_bytes(data)
                    print(f"  -> Saved Windows package: {path.name} ({len(data)} bytes)")

            if b'name="linux"' in part and b'filename=' in part:
                idx = part.find(b"\r\n\r\n")
                if idx > 0:
                    start = idx + 4
                    end = part.rfind(b"\r\n")
                    data = part[start:end]
                    path = UPLOAD_DIR / f"HanoiGame_{version}_Linux_{timestamp}.tar.gz"
                    path.write_bytes(data)
                    print(f"  -> Saved Linux package: {path.name} ({len(data)} bytes)")

        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()
        self.wfile.write(b'{"ok":true,"version":"' + version.encode() + b'"}')

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "GET,POST,OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "Content-Type")
        self.end_headers()


if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Hanoi Game Log Server")
    p.add_argument("--port", type=int, default=8080, help="Listen port")
    p.add_argument("--public", action="store_true", help="Listen on all interfaces")
    args = p.parse_args()

    host = "0.0.0.0" if args.public else "127.0.0.1"
    server = HTTPServer((host, args.port), Handler)
    print(f"=== Hanoi Game Log Server ===")
    print(f"Listening on http://{host}:{args.port}")
    print(f"Logs: {LOG_DIR}")
    print(f"Uploads: {UPLOAD_DIR}")
    print(f"Version: {VERSION_FILE.read_text().strip()}")
    print(f"Ctr+C to stop")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nShutting down...")
        server.shutdown()

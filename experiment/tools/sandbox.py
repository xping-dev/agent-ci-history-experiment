#!/usr/bin/env python3
"""Stand-in for the two external sandboxes the tests call. Lives outside the repo the agent sees.

Payment provider:
  POST /v1/authorizations        -> {"id": "...", "status": "authorized"}, after the current delay
Parcel carrier:
  GET  /carrier/v1/quotes?...    -> {"service": "...", "price": ...}; needs the token in X-Api-Token
                                    or in "Authorization: Bearer"; 401 without it or during an outage
Controls (used when seeding history):
  GET  /__delay?ms=N             -> payment delay for following requests
  GET  /__carrier?mode=ok|outage -> outage: the carrier rejects every call with 401
"""
import json, sys, time, uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

CARRIER_TOKEN = "sbx_4f1c9a7e2d"
delay_ms = 0
carrier_mode = "ok"

class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def _send(self, code, body):
        data = json.dumps(body).encode()
        try:
            self.send_response(code)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def do_GET(self):
        global delay_ms, carrier_mode
        url = urlparse(self.path)
        query = parse_qs(url.query)
        if url.path == "/__delay":
            delay_ms = int(query.get("ms", ["0"])[0])
            self._send(200, {"delay_ms": delay_ms})
        elif url.path == "/__carrier":
            carrier_mode = query.get("mode", ["ok"])[0]
            self._send(200, {"carrier_mode": carrier_mode})
        elif url.path == "/carrier/v1/quotes":
            token = self.headers.get("X-Api-Token") or self.headers.get("Authorization", "").removeprefix("Bearer ").strip()
            if carrier_mode != "ok" or token != CARRIER_TOKEN:
                self._send(401, {"error": "invalid or expired token"})
                return
            weight = float(query.get("weightKg", ["1"])[0])
            self._send(200, {"service": "freight" if weight > 20 else "standard", "price": round(9.5 + 0.85 * weight, 2)})
        else:
            self._send(404, {"error": "not found"})

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0))
        self.rfile.read(length)
        if urlparse(self.path).path != "/v1/authorizations":
            self._send(404, {"error": "not found"})
            return
        time.sleep(delay_ms / 1000)
        self._send(200, {"id": "auth_" + uuid.uuid4().hex[:12], "status": "authorized"})

    def log_message(self, *args):
        pass

if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8089
    ThreadingHTTPServer(("0.0.0.0", port), Handler).serve_forever()

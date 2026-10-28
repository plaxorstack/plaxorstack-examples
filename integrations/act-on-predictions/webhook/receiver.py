# Receives a Webhook action's deliveries and checks each one really came from PlaxorStack.
# Standard library only. Set PLAXOR_WEBHOOK_SECRET to the card's signing secret first.
#
#   python receiver.py            # listens on http://localhost:8080/hooks/prediction
import hashlib
import hmac
import json
import os
import time
from http.server import BaseHTTPRequestHandler, HTTPServer

secret = os.environ["PLAXOR_WEBHOOK_SECRET"].encode("utf-8")
port = int(os.environ.get("PORT", "8080"))

# Every retry of one delivery carries the same id. Keep these in your database in production: this
# set is forgotten when the process restarts.
seen = set()


def is_genuine(timestamp, body, signature):
    if not timestamp or not signature:
        return False

    # Refuse anything older than five minutes, so a captured delivery can't be sent again later. The
    # timestamp is inside what is signed, so it can't be changed without breaking the signature.
    if not timestamp.isdigit() or abs(time.time() - int(timestamp)) > 300:
        return False

    expected = hmac.new(secret, timestamp.encode("utf-8") + b"." + body, hashlib.sha256).hexdigest()
    return hmac.compare_digest(expected, signature.lower())


class Handler(BaseHTTPRequestHandler):
    def do_POST(self):
        # Sign the bytes exactly as they arrived: parsing and re-serialising the JSON first changes them.
        body = self.rfile.read(int(self.headers.get("Content-Length", 0)))

        if not is_genuine(self.headers.get("X-Training-Timestamp"), body, self.headers.get("X-Training-Signature")):
            self.send_response(401)
            self.end_headers()
            return

        delivery = self.headers.get("X-Training-Delivery-Id")

        if delivery not in seen:
            seen.add(delivery)
            # Your work goes here. Answer quickly: a slow answer is retried as a failure.
            print(f"{delivery}: {json.loads(body or b'{}')}")

        self.send_response(200)
        self.end_headers()

    do_PUT = do_PATCH = do_POST


print(f"Listening on http://localhost:{port}/hooks/prediction")
HTTPServer(("", port), Handler).serve_forever()

# Asks your model whether a server reading is unusual.
# Usage: python predict.py [requests_per_min] [cpu_percent]   (default: 250 requests, 86% CPU)
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]
requests_per_min = sys.argv[1] if len(sys.argv) > 1 else "250"
cpu_percent = sys.argv[2] if len(sys.argv) > 2 else "86"

response = requests.post(
    f"{api_url}/predict",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "inputs": {"requests_per_min": requests_per_min, "cpu_percent": cpu_percent}},
    timeout=30,
)

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

unusual = "yes" if result["predictedLabel"] == "True" else "no"
print(f"{requests_per_min} requests/min at {cpu_percent}% CPU — unusual: {unusual} (score {result['score']:.2f})")

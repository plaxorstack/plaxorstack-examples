# Asks your model a bean's variety from its length and width.
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]

response = requests.post(
    f"{api_url}/predict",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "inputs": {"length_px": "409", "width_px": "236"}},
    timeout=30,
)

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

print(f"Variety: {result['predictedLabel']}")

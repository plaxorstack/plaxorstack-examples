# Asks your model which topics a product review is about. A review can be about several.
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]

response = requests.post(
    f"{api_url}/predict/text",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "text": "The jacket is lovely and warm but it took three weeks to arrive and the sleeves are too short."},
    timeout=30,
)

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

# Empty when no topic is likely enough — a review about none of them.
topics = result.get("predictedLabels") or []
print(f"Topics: {', '.join(topics) if topics else '(none)'}")

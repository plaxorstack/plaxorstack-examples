# Searches your help articles for the ones that answer a customer's question.
# Usage: python predict.py ["your question"]   (default: "where is my parcel")
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]
question = sys.argv[1] if len(sys.argv) > 1 else "where is my parcel"

response = requests.post(
    f"{api_url}/predict/text",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "text": question, "top": 3, "minScore": 0.3},
    timeout=30,
)

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

print(f"> {question}")

# Empty, not an error, when nothing is close enough: hand over to a person.
if not result["matches"]:
    print("  No article answers that. Passing you to our team.")

for match in result["matches"]:
    payload = match.get("payload") or {}
    print(f"  {match['score']:.2f}  {payload.get('question', match['id'])}")
    print(f"        {payload.get('link', '')}")

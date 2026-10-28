# Sends a night's tickets to your model in one batch and saves the results file its action writes.
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
#
#   python batch-results.py                      # ../data/overnight-tickets.txt, one ticket per line
#   python batch-results.py path/to/tickets.txt
import os
import sys
import time

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]

here = os.path.dirname(os.path.abspath(__file__))
path = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "data", "overnight-tickets.txt")

with open(path, encoding="utf-8") as f:
    tickets = [line.strip() for line in f if line.strip()][:32]  # a batch holds at most 32 texts

response = requests.post(
    f"{api_url}/predict/text/batch",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "texts": tickets},
    timeout=60,
)

# A rejected key (401) has no body, so don't assume JSON.
results = response.json() if response.content else {}

if not response.ok:
    sys.exit(f"{response.status_code}: {results.get('message', response.reason)}")

for ticket, result in zip(tickets, results):
    label = result["predictedLabel"] if result.get("success") else f"failed: {result.get('message')}"
    print(f"{label:<10} {ticket[:70]}")

# One header per Results file action that matched at least one row. None: no such action on this
# model, it is set to Ask first, or no row met its condition.
links = response.raw.headers.getlist("X-Result-File")

if not links:
    print("\nNo results file: no Run immediately action matched any row.")
    sys.exit(0)

for number, link in enumerate(links, start=1):
    # The worker writes the file a few seconds after the batch answers. Until then: 202 + Retry-After.
    # The link is its own authorization, so the API key is not sent with it.
    for _ in range(24):
        download = requests.get(link, timeout=60)

        if download.status_code != 202:
            break

        time.sleep(int(download.headers.get("Retry-After", "5")))

    if download.status_code != 200:
        sys.exit(f"Results file {number}: {download.status_code} {download.text[:200]}")

    kind = download.headers.get("Content-Type", "")
    name = f"results-{number}.{'zip' if 'zip' in kind else 'pdf'}"

    with open(name, "wb") as out:
        out.write(download.content)

    print(f"\nSaved {name} ({len(download.content):,} bytes)")

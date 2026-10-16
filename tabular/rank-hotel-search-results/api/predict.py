# Orders the hotels of a new search so the ones a traveller is most likely to book come first.
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]

# (hotel, distance from the centre in km, guest rating out of 10)
candidates = [
    ("Harbour View", 0.6, 7.1),
    ("Hilltop Residence", 9.5, 9.6),
    ("Station Lodge", 1.1, 8.8),
    ("Lakeside Retreat", 4.0, 9.2),
    ("City Nest", 0.3, 6.4),
]

ranked = []

for hotel, distance, rating in candidates:
    response = requests.post(
        f"{api_url}/predict",
        headers={"X-Api-Key": api_key},
        json={
            "trainingTypeId": int(model_id),
            "inputs": {"distance_km": str(distance), "guest_rating": str(rating)},
        },
        timeout=30,
    )

    # A rejected key (401) has no body, so don't assume JSON.
    result = response.json() if response.content else {}

    if not response.ok or not result.get("success"):
        sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

    ranked.append((result["score"], hotel, distance, rating))

# A ranking score has no unit: it only says which item goes above which.
for position, (score, hotel, distance, rating) in enumerate(sorted(ranked, reverse=True), start=1):
    print(f"{position}. {hotel:<18} {distance:>4} km  {rating}/10   score {score:.2f}")

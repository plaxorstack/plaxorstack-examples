# Sends a photo of a fabric swatch to your model and prints every tag it gives it.
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
#
#   python predict.py                         # a held-out photo from ../data/try
#   python predict.py path/to/your-swatch.jpg   # your own photo
import os
import sys

import requests

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]

here = os.path.dirname(os.path.abspath(__file__))
photo = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "data", "try", "red-white-striped.jpg")

with open(photo, "rb") as image:
    response = requests.post(
        f"{api_url}/predict/image",
        headers={"X-Api-Key": api_key},
        data={"trainingTypeId": model_id},
        files={"image": (os.path.basename(photo), image, "image/jpeg")},
        timeout=60,
    )

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

# Every tag likely enough, each against its own threshold. Empty when none is.
tags = result.get("predictedLabels") or []
print(f"{os.path.basename(photo)}: {', '.join(tags) if tags else '(no tags)'}")

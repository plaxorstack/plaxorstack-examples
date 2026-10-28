# Asks your model for a shop's next 14 days of sales.
# Usage: python predict.py [shop] [promotion days]   e.g. python predict.py station-kiosk 5,12
# Needs: pip install requests
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
import os
import sys
from datetime import date

import requests

HORIZON = 14

api_url = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
api_key = os.environ["PLAXOR_API_KEY"]
model_id = os.environ["PLAXOR_MODEL_ID"]
shop = sys.argv[1] if len(sys.argv) > 1 else "harbour-street"
promotions = {int(d) for d in sys.argv[2].split(",") if d} if len(sys.argv) > 2 else set()

# One [on_promotion] per day ahead: the model was trained with it, so it needs the plan.
covariates = [[1 if day in promotions else 0] for day in range(1, HORIZON + 1)]

response = requests.post(
    f"{api_url}/predict/forecast",
    headers={"X-Api-Key": api_key},
    json={"trainingTypeId": int(model_id), "horizon": HORIZON, "series": shop, "covariates": covariates},
    timeout=30,
)

# A rejected key (401) has no body, so don't assume JSON.
result = response.json() if response.content else {}

if not response.ok or not result.get("success"):
    sys.exit(f"{response.status_code}: {result.get('message', response.reason)}")

forecast = result["forecast"]
print(f"{forecast['series']}, {forecast['frequency']} — next {len(forecast['points'])} days")

for point in forecast["points"]:
    day = date.fromisoformat(point["date"][:10])
    print(f"{day}  {day:%a}  {point['value']:5.0f} loaves   likely {point['lower']:.0f}–{point['upper']:.0f}")

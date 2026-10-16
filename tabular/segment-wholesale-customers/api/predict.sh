#!/bin/sh
# Asks your model which customer segment a buyer belongs to.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"inputs\": {\"fresh_food_spend\": \"5000\", \"grocery_spend\": \"30000\"}}"
echo

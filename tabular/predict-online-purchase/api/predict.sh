#!/bin/sh
# Asks your model whether a web-shop visitor will buy.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"inputs\": {\"page_value\": \"35.5\", \"visitor_type\": \"new\"}}"
echo

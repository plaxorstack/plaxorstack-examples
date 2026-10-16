#!/bin/sh
# Asks your model which topics a product review is about. A review can be about several.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/text" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"text\": \"The jacket is lovely and warm but it took three weeks to arrive and the sleeves are too short.\"}"
echo

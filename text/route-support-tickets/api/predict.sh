#!/bin/sh
# Asks your model which team should handle a support ticket.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/text" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"text\": \"Hello, my parcel was marked as delivered yesterday but it is not here. The courier is DHL.\"}"
echo

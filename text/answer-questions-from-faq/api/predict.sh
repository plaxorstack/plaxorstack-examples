#!/bin/sh
# Searches your help articles for the ones that answer a customer's question.
# Usage: sh predict.sh ["your question"]   (default: "where is my parcel")
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"
question="${1:-where is my parcel}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/text" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"text\": \"$question\", \"top\": 3, \"minScore\": 0.3}"
echo

#!/bin/sh
# Asks your model whether a server reading is unusual.
# Usage: sh predict.sh [requests_per_min] [cpu_percent]   (default: 250 requests, 86% CPU)
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"
requests="${1:-250}"
cpu="${2:-86}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"inputs\": {\"requests_per_min\": \"$requests\", \"cpu_percent\": \"$cpu\"}}"
echo

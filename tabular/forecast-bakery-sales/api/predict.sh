#!/bin/sh
# Asks your model for a shop's next 14 days of sales.
# Usage: sh predict.sh [shop] [promotion days]   e.g. sh predict.sh station-kiosk 5,12
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"
shop="${1:-harbour-street}"
promotions=",${2:-},"
horizon=14

# One [on_promotion] per day ahead: the model was trained with it, so it needs the plan.
covariates=""
day=1
while [ $day -le $horizon ]; do
  case "$promotions" in *",$day,"*) value=1 ;; *) value=0 ;; esac
  covariates="$covariates${covariates:+, }[$value]"
  day=$((day + 1))
done

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/forecast" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"horizon\": $horizon, \"series\": \"$shop\", \"covariates\": [$covariates]}"
echo

#!/bin/sh
# Scores each hotel of a new search; sort by "score", highest first, to get the order to show them in.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

# hotel  distance_km  guest_rating
while read -r hotel distance rating; do
  printf '%-18s ' "$hotel"
  curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict" \
    -H "X-Api-Key: $PLAXOR_API_KEY" \
    -H "Content-Type: application/json" \
    -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"inputs\": {\"distance_km\": \"$distance\", \"guest_rating\": \"$rating\"}}"
  echo
done <<'EOF'
Harbour_View      0.6 7.1
Hilltop_Residence 9.5 9.6
Station_Lodge     1.1 8.8
Lakeside_Retreat  4.0 9.2
City_Nest         0.3 6.4
EOF

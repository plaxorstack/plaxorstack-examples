#!/bin/sh
# Sends three tickets to your model in one batch, prints the raw answer, and saves the results file
# its action writes. The other languages read ../data/overnight-tickets.txt; plain sh can't safely
# turn a file into JSON, so the tickets are written out here.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

headers=$(mktemp)
trap 'rm -f "$headers"' EXIT

curl -sS -D "$headers" -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/text/batch" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"trainingTypeId\": $PLAXOR_MODEL_ID, \"texts\": [
        \"I was charged twice for order #58213, please refund one of the payments.\",
        \"Tracking says the parcel was delivered on Monday but nothing came.\",
        \"I was refunded 20 euros but I paid 35. Where is the rest?\"]}"
echo

# One X-Result-File header per Results file action that matched at least one row.
links=$(grep -i '^x-result-file:' "$headers" | cut -d' ' -f2 | tr -d '\r')

if [ -z "$links" ]; then
  echo "No results file: no Run immediately action matched any row."
  exit 0
fi

n=0
for link in $links; do
  n=$((n + 1))
  # 202 until the worker has written it, a few seconds. The link is its own authorization: no key.
  for attempt in 1 2 3 4 5 6 7 8 9 10 11 12; do
    status=$(curl -sS -L -o "results-$n.download" -w '%{http_code}' "$link")
    [ "$status" != "202" ] && break
    sleep 5
  done

  if [ "$status" != "200" ]; then
    echo "Results file $n: $status $(cat "results-$n.download")" >&2
    rm -f "results-$n.download"
    exit 1
  fi

  # A PDF starts with %PDF; anything else this route serves is a ZIP.
  if head -c 4 "results-$n.download" | grep -q '%PDF'; then ext=pdf; else ext=zip; fi
  mv "results-$n.download" "results-$n.$ext"
  echo "Saved results-$n.$ext"
done

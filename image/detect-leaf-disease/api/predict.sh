#!/bin/sh
# Sends a photo of a bean leaf to your model and prints what it sees.
# Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
#
#   sh predict.sh                         # a held-out photo from ../data/try
#   sh predict.sh path/to/your-leaf.jpg   # your own photo
: "${PLAXOR_API_KEY:?Set PLAXOR_API_KEY to your API key}"
: "${PLAXOR_MODEL_ID:?Set PLAXOR_MODEL_ID to the number after Model #}"

photo="${1:-$(dirname "$0")/../data/try/bean_rust-1.jpg}"

curl -sS -X POST "${PLAXOR_API_URL:-https://api.plaxorstack.com}/predict/image" \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -F "trainingTypeId=$PLAXOR_MODEL_ID" \
  -F "image=@$photo;type=image/jpeg"
echo

# Scores every row of a CSV with your model and writes the predictions next to the original columns.
# Needs: pip install requests. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.
#
#   python score.py input.csv output.csv                  # tabular: every column is sent as an input
#   python score.py input.csv output.csv --text message   # text model: sends that one column
#
# Rows the API couldn't score (the monthly allowance ran out, a bad value) are written with their
# error, and also to <output>.failed.csv, so you can re-run just those later.
import argparse
import csv
import os
import random
import sys
import time

import requests

API_URL = os.environ.get("PLAXOR_API_URL", "https://api.plaxorstack.com")
API_KEY = os.environ.get("PLAXOR_API_KEY")
MODEL_ID = os.environ.get("PLAXOR_MODEL_ID")

TABULAR_BATCH = 1000
TEXT_BATCH = 32
TEXT_BATCH_CHARS = 200_000
RETRY_STATUS = {429, 500, 502, 503, 504}


def post_with_retry(session, path, payload, attempts=5):
    for attempt in range(attempts):
        try:
            response = session.post(f"{API_URL}{path}", json=payload, timeout=120)
            # A 429 with a body is the monthly quota, not the per-minute limit: waiting won't help.
            if response.status_code not in RETRY_STATUS or (response.status_code == 429 and response.content):
                return response
            reason = f"HTTP {response.status_code}"
        except (requests.ConnectionError, requests.Timeout) as error:
            reason = type(error).__name__
        if attempt < attempts - 1:
            delay = min(60, 2 ** attempt * 2) + random.uniform(0, 1)
            print(f"  {reason}, retrying in {delay:.0f} s", file=sys.stderr)
            time.sleep(delay)
    sys.exit(f"Gave up after {attempts} attempts: {reason}")


def batches(rows, text_column):
    if text_column is None:
        for start in range(0, len(rows), TABULAR_BATCH):
            yield rows[start:start + TABULAR_BATCH]
        return
    batch, chars = [], 0
    for row in rows:
        length = len(row[text_column])
        if batch and (len(batch) == TEXT_BATCH or chars + length > TEXT_BATCH_CHARS):
            yield batch
            batch, chars = [], 0
        batch.append(row)
        chars += length
    if batch:
        yield batch


def main():
    parser = argparse.ArgumentParser(description="Score a CSV with a PlaxorStack model.")
    parser.add_argument("input")
    parser.add_argument("output")
    parser.add_argument("--text", metavar="COLUMN", help="for a text model: the column to send")
    args = parser.parse_args()

    if not API_KEY or not MODEL_ID:
        sys.exit("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.")

    with open(args.input, newline="", encoding="utf-8-sig") as f:
        reader = csv.DictReader(f)
        columns = reader.fieldnames or []
        rows = list(reader)

    if args.text and args.text not in columns:
        sys.exit(f"No column '{args.text}' in {args.input}. Columns: {', '.join(columns)}")

    session = requests.Session()
    session.headers["X-Api-Key"] = API_KEY
    out_columns = columns + ["predicted_label", "score", "probability", "error"]
    failed_path = os.path.splitext(args.output)[0] + ".failed.csv"
    scored = failed = 0

    with open(args.output, "w", newline="", encoding="utf-8") as out, \
         open(failed_path, "w", newline="", encoding="utf-8") as failed_out:
        writer = csv.DictWriter(out, fieldnames=out_columns)
        failed_writer = csv.DictWriter(failed_out, fieldnames=columns)
        writer.writeheader()
        failed_writer.writeheader()

        for batch in batches(rows, args.text):
            if args.text:
                path = "/predict/text/batch"
                payload = {"trainingTypeId": int(MODEL_ID), "texts": [row[args.text] for row in batch]}
            else:
                path = "/predict/batch"
                payload = {"trainingTypeId": int(MODEL_ID), "rows": batch}

            response = post_with_retry(session, path, payload)
            body = response.json() if response.content else {}

            if not response.ok:
                # The whole call was refused (bad key, quota already used up): every row fails the same way.
                message = body.get("message") if isinstance(body, dict) else None
                results = [{"success": False, "message": message or f"HTTP {response.status_code}"}] * len(batch)
            else:
                results = body

            # One result per row, in the order sent. Check each: the HTTP status is 200 even when the
            # monthly allowance ran out partway through the batch.
            for row, result in zip(batch, results):
                ok = result.get("success")
                writer.writerow({
                    **row,
                    "predicted_label": result.get("predictedLabel") or "",
                    "score": "" if result.get("score") is None else result["score"],
                    "probability": "" if result.get("probability") is None else result["probability"],
                    "error": "" if ok else result.get("message") or "failed",
                })
                if ok:
                    scored += 1
                else:
                    failed += 1
                    failed_writer.writerow(row)

            print(f"  {scored + failed:,} / {len(rows):,}", file=sys.stderr)

    print(f"Scored {scored:,} rows into {args.output}.")
    if failed:
        print(f"{failed:,} rows failed; they're in {failed_path}. Re-run with that file as the input.")
    else:
        os.remove(failed_path)


if __name__ == "__main__":
    main()

# Batch-score a CSV

**Use it for:** scoring a whole file overnight, such as this month's leads, yesterday's tickets or
the full product catalogue, and getting a CSV back with the predictions added as columns.

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
pip install requests

# a tabular model: every column of the file is sent as an input
python score.py sessions.csv sessions-scored.csv

# a text model: name the column to send
python score.py tickets.csv tickets-routed.csv --text ticket_text
```

```
  1,000 / 2,400
  2,000 / 2,400
  2,400 / 2,400
Scored 2,400 rows into sessions-scored.csv.
```

The output has every original column plus:

| Column | Holds |
|---|---|
| `predicted_label` | The category, the yes/no, the cluster |
| `score` | The number for regression; the winning category's confidence for classification |
| `probability` | For binary classification, the chance of `True` |
| `error` | Empty when the row was scored, otherwise why not |

## Try it on an example

With the [online purchase](../../tabular/predict-online-purchase/README.md) model, score its own
data. The `purchased` column is sent too and ignored, since the model reads only the columns it was
trained on, so you can compare its guess with what really happened:

```sh
python score.py ../../tabular/predict-online-purchase/data/store-sessions.csv sessions-scored.csv
```

Each row counts as a prediction, and that file has 12,330 of them. On the free trial, cut it to a few
hundred rows first.

With the [ticket routing](../../text/route-support-tickets/README.md) model, score its own data to
see the batch text path:

```sh
python score.py ../../text/route-support-tickets/data/tickets.csv routed.csv --text ticket_text
```

## How it works

- **Batches.** Tabular rows go to `/predict/batch`, 1,000 at a time. Texts go to
  `/predict/text/batch`, 32 at a time and never more than 200,000 characters per call. A batch counts
  as **one request** against the per-minute limit, so 100,000 rows are 100 requests.
- **Retries.** A 429 with no body (the per-minute limit), a 5xx, or a timeout is retried after 2,
  4, 8 and 16 seconds. A 429 **with** a message means the month's predictions are used up, so it
  isn't retried.
- **Partial failures.** If the allowance runs out partway through, the batch still answers 200,
  with `success: false` on the rows it couldn't score. The script checks every row, writes the error,
  and copies the failed rows to `<output>.failed.csv`. Re-run with that file next month or after an
  upgrade:

  ```sh
  python score.py sessions-scored.failed.csv sessions-scored-2.csv
  ```

- **Each row counts as one prediction** against your monthly allowance. Check your plan before you
  score a million rows.

## Make it yours

- Only the columns your model was trained on matter. The API matches them **by name**, so the file's
  header must use the same column names as the file you trained on.
- To score on a schedule, run it from cron, a GitHub Actions `schedule`, or Azure/AWS functions.
  Keep the key in that platform's secret store.
- More on retries, quotas and keys: [production guide](../../production/README.md).

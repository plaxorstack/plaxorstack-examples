# Spot unusual server load

**Task:** Anomaly detection — no labels needed · **Data:** a week of server monitoring, 2,016
readings · **Time:** ~10 minutes

A company's internal ordering system logs two numbers every five minutes: how many requests it
serves and how busy its CPU is. A fixed alert like *"CPU above 80%"* is useless here. The busy
afternoon runs at 75% every day, while 60% at three in the morning means something is wrong.

What matters is the **link** between the two numbers. More traffic should mean more CPU. Anomaly
detection learns that link from ordinary readings, and flags a reading that breaks it:

- a CPU working hard with **no traffic** behind it: a stuck job, a runaway script, a miner;
- **heavy traffic with an idle CPU**: requests failing before they do any work;
- a CPU far **busier than its traffic** explains: a slow query, a bad deploy.

**Nobody has to label anything.** That's the point. Incidents are rare and new kinds keep turning
up, so you can't collect enough examples of each to train a classifier. You can always collect a
week of normal operation.

The same recipe catches anything that breaks a usual pattern: an invoice whose amount doesn't fit
its supplier, a payment unlike a customer's others, a sensor reading out of line with its
neighbours, a shift with an odd number of returns.

## The data

`data/server-metrics.csv`: one reading every 5 minutes for 7 days.

| Column | Meaning | Example |
|---|---|---|
| `time` | When the reading was taken | `2026-09-08 14:30` |
| `requests_per_min` | Requests served per minute | `1987` |
| `cpu_percent` | Average CPU use, % | `74.3` |

```
time,requests_per_min,cpu_percent
2026-09-07 00:00,215,11.9
2026-09-07 00:05,231,12.5
```

Traffic is quiet at night (~220 requests a minute, ~12% CPU), busy on weekday office hours (~2,000
requests, ~75% CPU), and quiet again at the weekend. **11 readings are incidents.** They aren't
marked in the file, because in real life nobody would have marked them either.

This dataset was written for the example: real monitoring data is private. See
[DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

**Machine Learning** → under the tabular types, click **Anomaly Detection**. Name it `Server load`
and drop in `data/server-metrics.csv`.

## 2. Match the columns

| Your column | Field | Why |
|---|---|---|
| `requests_per_min` | **Features** | A number |
| `cpu_percent` | **Features2** | A second number. The model learns how it moves with the first |
| `time` | *(leave unmapped)* | The model judges a reading by its numbers. Mapping the time would teach it the timestamps of one particular week |

There's **no Label**. If you map one anyway it's ignored, and the page says so.

Click **Save**.

## 3. Train

**Trainings** → open it → **Algorithms** tab → **Actions → Train**. Anomaly detection trains on the
CPU in a few seconds.

**How it decides.** Training puts the threshold where **about 1% of the training readings** fall
above it. This is deliberate: the model assumes the week you gave it was mostly normal, and calls
the strangest 1% unusual. On this file that's around 20 readings: the 11 incidents plus a handful
from the minutes when traffic ramps up at 8 a.m. or down at 6 p.m.

**The metric** is *Average anomaly score* on readings held out from training. **Lower is better**:
it means the learned pattern describes data it wasn't fitted on.

## 4. Try it in the browser

Click **Predict** on the trained row:

| requests_per_min | cpu_percent | You should see | Why |
|---|---|---|---|
| `1900` | `72` | **False**, score near 0 | A busy afternoon. High CPU, but the traffic explains it |
| `230` | `12` | **False**, score near 0 | A quiet night |
| `250` | `86` | **True**, score ~0.9 | 86% CPU with almost no traffic |
| `2000` | `13` | **True**, score ~0.9 | Peak traffic and an idle CPU: requests are failing |
| `1400` | `97` | **True** | Far busier than that traffic explains |
| `300` | `50` | **True** | Half the CPU gone with nothing to show for it |

The **score** runs from 0 (exactly the usual pattern) to 1 (nothing in common with it).
`predictedLabel` is `True` when the score is above the threshold training chose.

## 5. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"      # Model # on the AI Board
cd api
python predict.py                  # 250 requests, 86% CPU
python predict.py 1900 72          # a busy afternoon
```

Also `sh predict.sh [requests] [cpu]`, `node predict.mjs [requests] [cpu]`,
`dotnet run predict.cs -- [requests] [cpu]`.

```
250 requests/min at 86% CPU — unusual: yes (score 0.94)
1900 requests/min at 72% CPU — unusual: no (score 0.02)
```

```json
POST https://api.plaxorstack.com/predict

{ "trainingTypeId": 1234, "inputs": { "requests_per_min": "250", "cpu_percent": "86" } }
```

Read `predictedLabel` (`"True"` = unusual) and `score`. In production you'd call this from the job
that collects your metrics, every few minutes, and page someone only when a reading comes back
`True`. Even better, page only on two in a row, since real incidents last longer than one reading.
To check a whole day in one go, send up to 1,000 readings to `/predict/batch`:

```json
POST https://api.plaxorstack.com/predict/batch

{ "trainingTypeId": 1234, "rows": [
    { "requests_per_min": "215", "cpu_percent": "11.9" },
    { "requests_per_min": "240", "cpu_percent": "86" }
] }
```

## 6. On a web page

The chat widget works with this model exactly as in
[bike rentals, step 6](../forecast-bike-rentals/README.md#6-put-it-on-a-web-page). An operator can
ask *"is 300 requests at 50% CPU normal?"*. For alerting, call the API from your monitoring instead.

## Make it yours

Export a stretch of **normal** operation: a week or a month, at least a few hundred rows. Choose the
numbers that should move together: traffic and CPU, order value and item count, hours worked and
units produced. Map the first to **Features** and the second to **Features2**. If what matters is
a number and a word (an amount and a supplier, a duration and a shift), map the word to
**Category** instead of Features2.

Two things to know:

- **Train on mostly normal data.** If half your week was an outage, the outage becomes normal.
  Cut out known incidents before you upload.
- **Retrain when normal changes.** After you add servers or launch a feature, the old pattern
  goes stale and readings start getting flagged for no reason.

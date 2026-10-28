# Forecast bakery sales

**Task:** Time-series forecasting — the next days of a number · **Data:** 21 months of daily sales
at three shops · **Time:** ~10 minutes · **Trains on:** CPU

A small bakery chain bakes every loaf before the shop opens. Bake too few and the shelves are empty
by noon; bake too many and the rest is thrown away. What they need every evening is **how many
loaves each shop will sell on each of the next two weeks' days** — and how sure that number is.

That is a different question from [bike rentals](../forecast-bike-rentals/README.md). There you give
the model an hour and a temperature and it answers one number. Here you give it nothing but a shop's
own **history**, and it continues the line: the weekly rhythm, the slow growth, the effect of a
promotion you have already planned.

The same recipe forecasts anything measured at regular intervals: orders a day, calls an hour,
sign-ups a week, energy use, stock to reorder.

## The data

`data/bakery-sales.csv` — every day from 1 January 2025 to 30 September 2026, for three shops.

| Column | Meaning | Example |
|---|---|---|
| `date` | The day | `2026-09-26` |
| `shop` | Which shop — each one is its own series | `harbour-street` |
| `loaves_sold` | Loaves sold that day — **what we forecast** | `297` |
| `on_promotion` | `1` on a day with a promotion, planned weeks ahead | `0` |

```
date,shop,loaves_sold,on_promotion
2025-01-01,harbour-street,160,0
2025-01-02,harbour-street,243,1
```

The three shops behave differently, which is why one model with a series per shop beats one total:

- **harbour-street** — the biggest, ~210 loaves a day, busiest on **Saturday**.
- **market-square** — ~135 a day, a sharp Saturday market peak.
- **station-kiosk** — ~140 a day from commuters, and **quiet at weekends**.

All three grow slowly, dip in the August holidays and rise before Christmas. A promotion day sells
about a third more. Every shop has a promotion on the first Tuesday of the month and another on its
own weekday every third week.

This dataset was written for the example: a real shop's till data is private. See
[DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. Sign in at [plaxorstack.com](https://plaxorstack.com) and open **Start training** (the **Training Pipeline** page).
2. Under the tabular types, click the **Time-Series Forecasting** card.
3. Name it `Bakery sales` and drop in `data/bakery-sales.csv`.

## 2. Match the columns

Drag each column on the left onto its field on the right:

| Your column | Field | Why |
|---|---|---|
| `loaves_sold` | **Label** | The number to forecast |
| `date` | **Features** | When it happened. The model works out that the rows are **daily** |
| `shop` | **Category** | Which series a row belongs to. Leave it out and every row must be a different date |
| `on_promotion` | **Features2** | A value you **know in advance**. A forecast may only use those: you'll send the planned promotions when you ask |

Click **Save**. Saving opens the training and checks the data: a missing day, the same day twice for
one shop, or a blank or text `loaves_sold` stops it with the row it found. This file passes.

## 3. Train

Click **Train** in the algorithm's row on the **Training** tab. Forecasting trains on the CPU in a
minute or two. **FastTree** is the default; **Find optimal algorithm** tries LightGBM as well and
keeps whichever forecasts better.

**How it's scored.** The model is tested the honest way: it is trained on the past, asked to forecast
the next **14 days** (the horizon for daily data), and compared with what was actually sold — three
times, at three different cut-off points. The metric is **MASE**, and **lower is better**:

- **below 1** — it beats the obvious guess, "the same as this day last week";
- **1** — it's no better than that guess;
- **above 1** — worse. Look for a gap or a change in the data before trusting it.

MAE is shown beside it, in loaves: how far off a typical day was.

## 4. Try it in the browser

Click **Predict** on the trained row and fill in:

- **Periods ahead:** `14`
- **Series:** `harbour-street`, written exactly as in the file
- **Known in advance: on_promotion:** one line per day ahead — fourteen lines of `0`, or put a `1`
  on the fifth line to plan a promotion five days out

The answer draws the last weeks of history, what the backtest predicted for days that have already
happened, and the forecast with its **likely range** — the band where 8 days out of 10 should land —
with a table of the days below it.

Things to look for:

| Shop | You should see | Why |
|---|---|---|
| `harbour-street` | Saturdays near **290**, weekdays near **180–210** | Its weekly rhythm |
| `station-kiosk` | Weekdays near **180**, Saturday and Sunday under **80** | Commuters don't travel at weekends |
| `market-square` | One day well above the rest each week | The Saturday market |
| any shop, with a promotion day set to `1` | That day about a third higher | The model learned what a promotion is worth |

## 5. Call it from code

Find the model's number on the **AI Board** (it reads **Model #1234**). Create an **API** key at
**Secrets**, then:

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                       # harbour-street, no promotions
python predict.py station-kiosk 5,12    # a promotion 5 and 12 days from now
```

| | Run |
|---|---|
| curl | `sh predict.sh [shop] [promotion days]` |
| Python | `python predict.py [shop] [promotion days]` |
| JavaScript | `node predict.mjs [shop] [promotion days]` |
| C# | `dotnet run predict.cs -- [shop] [promotion days]` |

```
harbour-street, Daily — next 14 days
2026-10-01  Thu   206 loaves   likely 188–224
2026-10-02  Fri   232 loaves   likely 212–252
2026-10-03  Sat   291 loaves   likely 266–316
...
```

(Your numbers will differ a little from run to run of training.)

Forecasts have their own route, because the question isn't a row of inputs:

```json
POST https://api.plaxorstack.com/predict/forecast
X-Api-Key: <your API key>

{
  "trainingTypeId": 1234,
  "horizon": 14,
  "series": "harbour-street",
  "covariates": [[0], [0], [0], [0], [1], [0], [0], [0], [0], [0], [0], [0], [0], [0]]
}
```

- **`horizon`** — days ahead, up to twice the trained horizon (28 here). `0` means the trained 14.
- **`series`** — the shop. Needed whenever the model was trained on more than one series.
- **`covariates`** — one array per day ahead, one value per known-ahead column, in the order you
  mapped them (Features2, then Features3). Needed whenever the model was trained with them: a
  forecast can't guess next week's promotions.

The answer is in `forecast`:

```json
{
  "success": true,
  "forecast": {
    "series": "harbour-street",
    "frequency": "Daily",
    "history":  [ { "date": "2026-09-30T00:00:00", "value": 197 } ],
    "backtest": [ { "date": "2026-09-30T00:00:00", "value": 201 } ],
    "points":   [ { "date": "2026-10-01T00:00:00", "value": 206, "lower": 188, "upper": 224 } ]
  }
}
```

`points` is the forecast, each day with its range. `history` and `backtest` are there to draw a
chart; they carry no range. One forecast request is metered as **one prediction**, however many days
it asks for. `/predict` and `/predict/batch` refuse a forecast model and point you here.

In production you'd call this once a day from whatever plans the bake, for each shop, and order
flour by `upper` rather than `value` if running out costs more than throwing away.

## Make it yours

Export one row per **period** — a day, a week, a month — with the date and the number. Several
series in one table need a column saying which series a row is (a shop, a product, a region); map it
to **Category**, up to 500 of them.

Before you upload:

- **No gaps.** A day the shop was closed is a row with `0`, not a missing row. A gap stops the check
  and names the date, because a model can't learn a weekly rhythm from a week with a day missing.
- **Enough history.** At least three full seasons plus a few weeks: for daily data with a weekly
  rhythm that's roughly two months, but a year or more is what lets it learn holidays.
- **Only add columns you'll know in advance.** A price you've set, a promotion you've planned, a
  public holiday: yes. Today's weather or visitor count: no, because you won't have them when you
  ask about next Tuesday. At most two.
- **Retrain as time passes.** A forecast continues from the last row it was trained on. Upload the
  newest weeks and retrain, or it keeps forecasting from where your file ended.

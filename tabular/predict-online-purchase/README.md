# Predict an online purchase

**Task:** Binary classification — yes or no · **Data:** 12,330 real web-shop sessions ·
**Time:** ~10 minutes

A web shop wants to know, while a visitor is still browsing, whether they're likely to buy. Then
it can offer help or a discount to the undecided, and leave the sure buyers alone.

The strongest signal is **page value**: Google Analytics' measure of how much the pages a visitor has
seen tend to lead to sales. The second is whether the visitor is **new or returning**.

The same recipe answers any yes/no question: will this customer cancel, will this invoice be paid
late, is this transaction fraudulent, will this lead convert.

## The data

`data/store-sessions.csv` — one row per visit to an online store over a year.

| Column | Meaning | Example |
|---|---|---|
| `page_value` | Average value of the pages viewed in the session | `35.5` |
| `visitor_type` | `returning`, `new` or `other` | `returning` |
| `purchased` | Did the visit end in a purchase? `true`/`false` — **what we predict** | `true` |

```
page_value,visitor_type,purchased
0,returning,false
0,returning,false
```

1,908 of the 12,330 visits (15%) ended in a purchase. Source and licence:
[DATA-LICENSES.md](../../DATA-LICENSES.md).

> Any two values work as a binary label: `true`/`false`, `yes`/`no`, `1`/`0`, or two words such as
> `Approved`/`Rejected`. The training page shows which one it takes as the positive outcome — here
> `true`, a purchase — and lets you change it.

## 1. Upload

**Machine Learning** → **Binary Classification**. Name it `Online purchase` and drop in
`data/store-sessions.csv`.

## 2. Match the columns

| Your column | Field | Why |
|---|---|---|
| `purchased` | **Label** | The yes/no to predict |
| `page_value` | **Features** | A number |
| `visitor_type` | **Category** | A word, so it goes to Category, not Features2 |

Click **Save**.

## 3. Train

**Trainings** → open it → **Training** tab → **Actions → Train**.

**What to expect:** in our test run, **89% accuracy** and an **AUC of 0.88**. Read the accuracy
with care: answering "no" every time would already score 85%, because most visits don't buy. AUC is
the better measure: 0.5 is guessing, 1.0 is perfect.

## 4. Try it in the browser

Click **Predict** on the trained row:

| page_value | visitor_type | You should see | Why |
|---|---|---|---|
| `0` | `returning` | **False**, probability < 0.1 | Only 3% of these buy |
| `35.5` | `new` | **True**, probability > 0.9 | New visitors who reach valuable pages nearly always buy |
| `35.5` | `returning` | probability **around 0.5** | Returning visitors browse more before they buy — could go either way |
| `5` | `new` | **False**, probability ~0.3 | Undecided — exactly who a discount is for |

## 5. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"      # Model # on the AI Board
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Will buy: True (probability 96%)
```

```json
POST https://api.plaxorstack.com/predict

{ "trainingTypeId": 1234, "inputs": { "page_value": "35.5", "visitor_type": "new" } }
```

Read `predictedLabel` (`"True"`/`"False"`) and `probability`, the chance of True. You choose the
threshold: a discount at probability 0.3 reaches more people than one at 0.6.

## 6. On a web page

The chat widget works with this model exactly as in
[bike rentals, step 6](../forecast-bike-rentals/README.md#6-put-it-on-a-web-page). In a shop,
though, you'd more likely call the API from your back end on each page view than show a chat.

## Make it yours

One yes/no column as **Label** (any two values), your strongest number as **Features**, and a
second number as **Features2** or a word as **Category**. Make sure both outcomes have plenty of
rows — a few hundred of the rarer one at least.

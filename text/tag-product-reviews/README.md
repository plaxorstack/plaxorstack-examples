# Tag product reviews

**Task:** Multi-label text classification · **Data:** 600 clothing-shop reviews, 5 topics ·
**Time:** ~10 minutes · **Trains on:** GPU

A clothing shop gets hundreds of reviews a week, and most are about **more than one thing**:

> *Ordered the jacket in blue. Great material, it still looks new after months. The parcel took almost
> two weeks to arrive.*

That review is about **quality** and **delivery**. An ordinary classifier has to pick one, so the
delivery complaint gets lost behind the praise. A **multi-label** model returns every topic that
applies, none, one or several. You can then count how many reviews mention sizing this month, or
send every delivery complaint to the logistics team, whatever else the review says.

The same recipe tags anything that can be about several things at once: support tickets
(`billing` + `urgent`), news articles by subject, job ads by skill, recipes by diet, contracts by
the clauses they contain.

## The data

`data/reviews.csv`

| Column | Meaning |
|---|---|
| `review` | The review text |
| `topics` | Every topic the review is about, separated by `;` — **what we predict** |

```
review,topics
The seams started coming apart after the first wash. Worth paying a little extra for. It arrived a day earlier than promised.,delivery;quality;price
Bought this shirt for my partner. The medium fits like a large.,sizing
```

| Topic | What reviewers say |
|---|---|
| `delivery` | fast or slow shipping, damaged parcels, couriers |
| `quality` | fabric, stitching, zips, fading after washing |
| `price` | value for money, sales, too expensive |
| `sizing` | runs small or large, sleeve and leg length, the size guide |
| `service` | customer service, returns, refunds |

163 reviews have one topic, 310 have two and 127 have three. Each review praises or complains about
each topic, and the model has to learn the topic either way.

This dataset was written for the example: see [DATA-LICENSES.md](../../DATA-LICENSES.md).

> Labels can be written as `delivery;quality` or as a JSON array `["delivery","quality"]`. At
> least some rows must have two or more labels, or it's an ordinary text classification and the
> page tells you so.

## 1. Upload

1. **Machine Learning** → under *Natural Language Processing*, click **Multi-label Text**.
2. Name it `Review topics` and drop in `data/reviews.csv`.
3. Under **Map your fields**, choose `review` as **Text** and `topics` as **Label**.
4. Click **Save and continue**.

## 2. Train

1. Open the **Training** tab and click **Train model**.
2. Choose the encoder. **MiniLM** is a good choice for a few hundred rows: it already knows English,
   so it doesn't have to learn the language from your 600 reviews. *From scratch* works too, but
   needs more data to do as well.
3. Click **Start training**. It takes a few minutes on a GPU.

**What to expect:** a **Micro F1** close to **1.0**. Micro F1 counts every topic of every held-out
review: a review tagged `quality` that should have been `quality;delivery` scores one right and one
missed. This set is easy because each topic has its own vocabulary. On real reviews, 0.75–0.90 is
good.

## 3. Try it in the browser

Open the **Prediction** tab, paste a review, and click **Run prediction**:

| Review | Should be tagged |
|---|---|
| *The jacket is lovely and warm but it took three weeks to arrive and the sleeves are too short.* | `quality`, `delivery`, `sizing` |
| *Cheap for what it is, and the customer service team swapped the size without any fuss.* | `price`, `service`, `sizing` |
| *Arrived next day. Fits perfectly.* | `delivery`, `sizing` |
| *I love the colour.* | maybe nothing: no topic is likely enough |

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Topics: quality, delivery, sizing
```

```json
POST https://api.plaxorstack.com/predict/text

{ "trainingTypeId": 1234, "text": "The jacket is lovely and warm but it took three weeks to arrive and the sleeves are too short." }
```

Read **`predictedLabels`**, a list that **can be empty**. `predictedLabel` and `probability` hold
the single strongest topic, for code that wants just one. To tag up to 32 reviews in one call, send
`{ "trainingTypeId": 1234, "texts": [ ... ] }` to `/predict/text/batch`.

> Text models are used through the API. The chat widget answers questions about tabular and image
> models, not text ones.

## Make it yours

A CSV with a text column and a column listing its labels, `;`-separated. A few things matter more
than the row count:

- **Tag everything that applies.** A review about price and sizing tagged only `price` teaches the
  model that sizing talk isn't about sizing.
- **Give each label at least 20–30 rows**, and preferably more.
- **Drop labels that are on almost every row.** A label on 95% of rows tells the model nothing, and
  the page warns about it. Over 100 labels is also flagged: group them first.

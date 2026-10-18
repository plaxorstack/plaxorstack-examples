# Recommend products

**Task:** Recommendation — what will this user like? · **Data:** 4,509 ratings by 250 customers of
40 products · **Time:** ~10 minutes

An online store wants a "recommended for you" row on every customer's home page. It has one thing
to go on: the **star ratings** customers have given products they bought.

A recommendation model learns tastes from those ratings. Customers who rated the same products the
same way are alike, so a product one of them loved is suggested to the others. It needs no product
descriptions and no customer profiles — only who rated what.

The same recipe suggests films, articles, courses, recipes, songs, or the next product to stock for
a shop.

## The data

`data/product-ratings.csv` — one row per rating.

| Column | Meaning | Example |
|---|---|---|
| `customer_id` | Who rated | `C0137` |
| `product` | What they rated | `Gaming mouse` |
| `rating` | Stars, 1–5 | `5` |

```
customer_id,product,rating
C0001,Reading lamp,4
C0001,Adjustable dumbbells,3
```

The 40 products fall into five interests: outdoor, kitchen, gaming, reading and fitness. Every
customer has rated 14–22 of them, so most products are still unrated for each customer — those are
what the model fills in. This dataset was written for the example; see
[DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

**Machine Learning** → **Recommendation**. Name it `Product recommendations` and drop in
`data/product-ratings.csv`.

## 2. Match the columns

| Your column | Field | Why |
|---|---|---|
| `customer_id` | **ForUser** | The user |
| `product` | **Category** | The item |
| `rating` | **Label** | How much they liked it |

Ids can be any text: `C0137`, an email hash, a product name, a SKU. Click **Save**.

## 3. Train

**Trainings** → open it → **Training** tab → **Actions → Train**.

**What to expect:** judge the model by its lists, not by an accuracy figure. Ratings are noisy, and
most possible pairs are never rated. What matters is that a customer's list holds things they would
plausibly want, which is what step 4 checks.

## 4. Try it in the browser

Click **Predict** and enter only a customer:

| customer_id | product | You should see |
|---|---|---|
| `C0137` | *(empty)* | **Capture card, Streaming microphone, Gaming mouse, Controller charging dock** at the top |

C0137 loves gaming and outdoor gear: they gave the 27-inch monitor and the XL mouse pad 5 stars, and
the tent and the sleeping bag too. They've rated every outdoor product already, but four gaming
products not yet, so those top the list. Products a customer has already rated are never
recommended.

Enter a product as well, for example `Gaming mouse`, and `score` is that customer's predicted rating
for it.

A customer the model has never seen gets the list for an average customer, marked as not
personalised. That's useful for a new visitor's first page.

## 5. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
4.2  Capture card
4.2  Streaming microphone
4.0  Gaming mouse
4.0  Controller charging dock
3.1  Yoga mat
```

```json
POST https://api.plaxorstack.com/predict

{ "trainingTypeId": 1234, "inputs": { "customer_id": "C0137" } }
```

| Field | Meaning |
|---|---|
| `recommendations` | Up to 20 `{ "item", "score" }`, best first, already-rated items left out |
| `recommendationsPersonalised` | `false` when the customer is unknown and the list is the average one |
| `score`, `personalised` | The predicted rating for the `product` you sent, if you sent one |

## Make it yours

Three columns: who (**ForUser**), what (**Category**), and how much they liked it (**Label**, a
number). There's no need for stars: a purchase count, minutes watched, or simply `1` for "bought"
works too. You need many users who each rated several items — a few thousand rows is a start.

# Segment wholesale customers

**Task:** Clustering — find groups, no labels needed · **Data:** 440 real wholesale customers ·
**Time:** ~5 minutes

A food wholesaler in Portugal sells to hundreds of businesses and treats them all the same. Some buy
mostly fresh produce, some mostly groceries, some are huge and some are tiny. Delivery schedules,
sales visits and offers would work better per group — but nobody has ever said what the groups are.

Clustering finds them. It needs **no label**: you give it the customers, and it puts similar ones
together.

The same recipe segments anything you can measure: shoppers by basket, machines by sensor profile,
stores by sales mix, users by how they use a product.

## The data

`data/customer-spend.csv` — annual spend per customer, in monetary units.

| Column | Meaning | Example |
|---|---|---|
| `fresh_food_spend` | Annual spend on fresh products | `12669` |
| `grocery_spend` | Annual spend on groceries | `7561` |

```
fresh_food_spend,grocery_spend
12669,7561
7057,9568
```

There's no answer column, and that's the point. Source and licence:
[DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

**Machine Learning** → **Clustering**. Name it `Customer segments` and drop in
`data/customer-spend.csv`.

## 2. Match the columns

| Your column | Field |
|---|---|
| `fresh_food_spend` | **Features** |
| `grocery_spend` | **Features2** |

There is no **Label** for clustering. Click **Save**.

## 3. Train

**Trainings** → open it → **Algorithms** tab → **Actions → Train**. It takes seconds.

**What to expect:** the platform makes **5 groups**. On this data, our test run found:

| Group | Customers | Typical fresh / grocery spend | Who they are |
|---|---|---|---|
| Small all-rounders | ~260 | 5,500 / 5,500 | Cafés, corner shops — the bulk of the list |
| Fresh-heavy | ~110 | 21,000 / 5,000 | Restaurants and hotels |
| Grocery-heavy | ~50 | 8,000 / 29,000 | Retailers |
| Large fresh buyers | ~20 | 46,000 / 5,500 | Big kitchens and caterers |
| One giant | 1 | 112,000 / 18,000 | An account big enough to be its own segment |

The original data also records each customer's channel: hotel/restaurant/café, or retail. The model
never saw it, yet the grocery-heavy group is **98% retail**, while the fresh-heavy groups are
**79–91% hotels, restaurants and cafés**. The groups it found are real.

**Group numbers are arbitrary.** "Cluster 3" just means "the third group"; train again and the same
group may get another number. Look at which customers landed together, then give each group a name.

## 4. Try it in the browser

| fresh_food_spend | grocery_spend | Lands with |
|---|---|---|
| `8000` | `4000` | the small all-rounders |
| `25000` | `3000` | the fresh-heavy |
| `5000` | `30000` | the grocery-heavy |

## 5. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Segment: cluster 3
```

Read `predictedLabel`, the group number. `scores` are the distances to each group's centre, so the
**smallest** is the group the customer belongs to. They're squared distances in the units of your
data, so they can be very large numbers. Don't read them as confidence, and ignore `score`.

## Make it yours

Map two measurements to **Features** and **Features2**. Groups form by distance, so a column measured
in thousands outweighs one measured in units — keep the two columns in similar ranges (for example
both in euros, or both as percentages).

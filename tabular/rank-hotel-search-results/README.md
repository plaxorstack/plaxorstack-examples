# Rank hotel search results

**Task:** Ranking — put the best first · **Data:** 500 searches × 8 hotels · **Time:** ~10 minutes

A travel site shows eight hotels for every search, and most travellers only look at the top three.
Which three? Not just the closest, and not just the best-reviewed: travellers trade one against the
other. A ranking model learns that trade from what past travellers actually did.

Ranking is different from regression: it doesn't predict a value for each hotel, it learns which
goes **above** which *within the same search*. That's why the data says which rows belong together.

The same recipe orders any list: search results in a shop, job candidates for a role, support
articles for a question, leads for a salesperson.

## The data

`data/hotel-searches.csv` — eight rows per search, one per hotel shown.

| Column | Meaning | Example |
|---|---|---|
| `search_id` | Which search the row belongs to | `S0001` |
| `hotel` | The hotel's name — not used by the model | `Harbour View` |
| `distance_km` | Distance from the city centre | `1.1` |
| `guest_rating` | Average review, out of 10 | `8.8` |
| `relevance` | What the traveller did — **what we learn from**: 3 booked · 2 saved · 1 opened · 0 ignored | `3` |

```
search_id,hotel,distance_km,guest_rating,relevance
S0001,Marina Bay Rooms,8.4,6.6,0
S0001,Hotel Aurora,6.2,9.5,3
```

This dataset was written for the example: real booking logs aren't published under an open
licence. See [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

**Machine Learning** → **Ranking**. Name it `Hotel ranking` and drop in `data/hotel-searches.csv`.

## 2. Match the columns

| Your column | Field | Why |
|---|---|---|
| `relevance` | **Label** | Higher is better |
| `search_id` | **ForUser** | Which rows are one list |
| `distance_km` | **Features** | First input |
| `guest_rating` | **Features2** | Second input |

Leave `hotel` unmapped. Click **Save**.

## 3. Train

**Trainings** → open it → **Algorithms** tab → **Actions → Train**.

**What to expect:** an **NDCG@3 around 0.87** in our test run. NDCG measures how close the top of the
model's order is to the ideal order: 1.0 means the hotels travellers chose are exactly the ones on
top.

## 4. Try it in the browser

A ranking score has no unit. Predict a few hotels and compare their scores — higher goes first:

| distance_km | guest_rating | Order you should get |
|---|---|---|
| `1.1` | `8.8` | 1st — close **and** well reviewed |
| `4.0` | `9.2` | 2nd — excellent, a little out |
| `0.6` | `7.1` | 3rd — central but average |
| `0.3` | `6.4` | 4th — central, poor reviews |
| `9.5` | `9.6` | 5th — superb but far out of town |

## 5. Call it from code

To rank a page of results, score each one and sort. The scripts do exactly that for the five hotels
above:

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: node predict.mjs · dotnet run predict.cs
```

```
1. Station Lodge       1.1 km  8.8/10   score 7.21
2. Lakeside Retreat    4.0 km  9.2/10   score 0.18
3. Harbour View        0.6 km  7.1/10   score -2.42
4. City Nest           0.3 km  6.4/10   score -7.56
5. Hilltop Residence   9.5 km  9.6/10   score -10.44
```

`predict.sh` prints each hotel's raw response; sort those by `score` yourself.

Each call is an ordinary `/predict` with the two inputs. You don't need to send a `search_id`: the
group only matters while training.

## Make it yours

You need past lists and what people did with them: one row per item shown, a column saying which
list it was in (**ForUser**), a number for how good it turned out (**Label** — clicks, purchases, a
0–3 grade), and two inputs that describe the item.

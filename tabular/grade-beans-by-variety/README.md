# Grade beans by variety

**Task:** Multiclass classification — one of several categories · **Data:** 13,611 measured dry beans,
7 varieties · **Time:** ~10 minutes

A seed company receives mixed lots of dry beans and must sort them by variety: each sells at a
different price, and a mixed bag is worth the cheapest bean in it. A camera over the conveyor
measures every bean, and a model decides which variety it is.

Two measurements already go a long way: a bean's **length** and its **width**.

The same recipe sorts anything into categories from measured values: product grade from sensor
readings, defect type, customer tier, the department a request belongs to.

## The data

`data/bean-measurements.csv` — beans photographed by a computer-vision system in Turkey.

| Column | Meaning | Example |
|---|---|---|
| `length_px` | Longest axis of the bean in the photo, in pixels | `409.2` |
| `width_px` | Shortest axis, in pixels | `236.1` |
| `variety` | Bean variety — **what we predict** | `Cali` |

| Variety | Beans | Typical length × width (px) |
|---|---|---|
| Dermason | 3,546 | 247 × 166 — small and slim |
| Sira | 2,636 | 299 × 191 |
| Seker | 2,027 | 251 × 202 — small and round |
| Horoz | 1,928 | 373 × 184 — long and narrow |
| Cali | 1,630 | 409 × 236 |
| Barbunya | 1,322 | 370 × 240 |
| Bombay | 522 | 593 × 374 — by far the largest |

Source and licence: [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

**Machine Learning** → **Multiclass Classification**. Name it `Bean varieties` and drop in
`data/bean-measurements.csv`.

## 2. Match the columns

| Your column | Field |
|---|---|
| `variety` | **Label** |
| `length_px` | **Features** |
| `width_px` | **Features2** |

Click **Save**.

## 3. Train

**Trainings** → open it → **Training** tab → **Actions → Train**.

**What to expect:** about **88% of beans** go to the right variety in our test run. The mistakes are
almost all between look-alikes: Dermason and Sira, or Barbunya and Cali. Bombay is never confused
with anything.

## 4. Try it in the browser

| length_px | width_px | You should see |
|---|---|---|
| `593` | `374` | **Bombay** |
| `373` | `184` | **Horoz** |
| `251` | `202` | **Seker** |
| `409` | `236` | **Cali** (Barbunya a close second) |
| `300` | `190` | **Sira** |

## 5. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Variety: Cali
```

Read `predictedLabel`. `scores` holds one score per variety if you want the runner-up as well.

## 6. On a web page

The widget works with this model as in
[bike rentals, step 6](../forecast-bike-rentals/README.md#6-put-it-on-a-web-page): *"What variety
is a bean 300 px long and 190 px wide?"*

## Make it yours

The category column is **Label**; two measurements are **Features** and **Features2**, or one
measurement and one word (**Features** + **Category**). Every category needs **at least 5 rows** to
train at all, and a few hundred to be reliable.

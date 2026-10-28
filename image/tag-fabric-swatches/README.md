# Tag fabric swatches

**Task:** Multi-label image classification — every tag that applies · **Data:** 480 swatch photos,
9 tags · **Time:** ~15 minutes · **Trains on:** GPU

A fabric shop lists hundreds of new swatches every season, and customers filter by colour and
pattern: *"navy, dotted"*, *"anything with yellow"*. Somebody has to tag every photo, and a red and
white stripe is **red**, **white** and **striped** at once. A category can't say that: a photo can
only be in one folder.

[Leaf disease](../detect-leaf-disease/README.md) answers *which one*. This model answers **which
ones** — any number of tags per photo, including none.

The same recipe tags anything where several things are true at once: what's in a room photo
(sofa, window, plant), which defects a part has, a product photo's colour, material and style, the
safety equipment someone is wearing.

## The data

| Path | What |
|---|---|
| `data/fabric-swatches.zip` | 480 swatch photos and `labels.csv`, the tags of each |
| `data/try/` | 6 more swatches that are **not** in the ZIP, named after their tags |

```
fabric-swatches.zip
├── labels.csv
└── swatches/
    ├── swatch-001.jpg
    ├── swatch-002.jpg
    └── … 480 photos
```

`labels.csv` — the image's path inside the ZIP, then its tags separated by `;`:

```
image,labels
swatches/swatch-001.jpg,black;green;red;striped
swatches/swatch-002.jpg,red;yellow;checked
swatches/swatch-003.jpg,yellow
swatches/swatch-004.jpg,navy;white;dotted
```

There are **9 tags**: six colours (`red`, `navy`, `green`, `yellow`, `white`, `black`) and three
patterns (`striped`, `dotted`, `checked`). A plain swatch has one colour and no pattern tag. A
striped one sometimes has three colours. Every tag is on 100–155 photos.

The swatches were drawn for this example, not photographed: no openly licensed photo collection is
tagged this way. Each is turned at a random angle, lit unevenly and given a woven grain, so the model
has to learn colours and patterns rather than positions. See [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. Sign in at [plaxorstack.com](https://plaxorstack.com) and open **Start training** (the **Training Pipeline** page).
2. Under *Computer Vision*, click **Multi-label Image Classification**.
3. Name it `Fabric tags` and drop in `data/fabric-swatches.zip`. The screen reads `labels.csv` and
   lists the 9 tags it found.
4. Click **Save and continue**.

There's nothing to map: the table says it all. The screen warns, rather than refuses, about photos in
the ZIP that the table doesn't list and rows naming photos that aren't there. It refuses only if
**no row has two or more tags** — then it isn't a multi-label problem, and
[Image Classification](../detect-leaf-disease/README.md) is the right card.

## 2. Train

**Training** tab → **Start training**. The dialog shows **Pretrained (ResNet-50)** and nothing else:
this task always starts from it. Image models train on a GPU.

The model learns a yes/no for every tag at once. Training then picks **a threshold per tag** on
photos held out from training: a rare tag might need only 0.3 to be worth saying, a common one 0.6.

**The metric** is **Micro F1**, from 0 to 1, higher is better. It counts every tag on every
held-out photo: tags the model gave that were right, tags it gave that were wrong, and tags it
missed. The run's summary also shows **precision and recall per tag**, which is where to look when
one tag is weak.

**What to expect:** colours are easy and patterns a little harder. The tag most often missed is a
colour that covers only a small part of the swatch, such as small dots.

## 3. Try it in the browser

Open the **Prediction** tab, upload a photo from `data/try/`, and click **Run prediction**. The
answer is the set of tags. The file names tell you what it should say:

| Photo | Should be |
|---|---|
| `red-white-striped.jpg` | red, white, striped |
| `navy-white-dotted.jpg` | navy, white, dotted |
| `green-yellow-checked.jpg` | green, yellow, checked |
| `black-plain.jpg` | black — and no pattern tag |
| `navy-yellow-white-striped.jpg` | navy, yellow, white, striped |
| `black-red-dotted.jpg` | black, red, dotted |

The **Test** tab shows each held-out photo's tags without a score, since a set of tags has no single
one.

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"      # Model # on the AI Board
cd api
python predict.py                              # sends data/try/red-white-striped.jpg
python predict.py ../data/try/black-plain.jpg
```

Also `sh predict.sh [photo]`, `node predict.mjs [photo]`, `dotnet run predict.cs -- [photo]`.

```
red-white-striped.jpg: red, striped, white
black-plain.jpg: black
```

It's the same route as single-label images — a multipart form:

```sh
curl -X POST https://api.plaxorstack.com/predict/image \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -F "trainingTypeId=$PLAXOR_MODEL_ID" \
  -F "image=@../data/try/red-white-striped.jpg"
```

Read **`predictedLabels`**, a list: every tag that cleared its own threshold, possibly none. For up
to 16 photos in one call, send them as repeated `images` fields to `/predict/image/batch`; each
result has its own `predictedLabels`.

In production you'd call this when a new swatch photo is uploaded and save the tags with the
product, so the shop's filters work without anyone typing them.

## Make it yours

ZIP your photos — in any folders you like — with a `labels.csv` beside them:

- **Column 1** is the photo's path inside the ZIP, exactly as the ZIP stores it.
- **The rest** are its tags: `red;white;striped` in one cell, one tag per column, or a JSON array
  such as `["red","white"]`. A header row like `image,labels` is fine.
- **A photo with no tags is skipped**, not taught "nothing applies". If "nothing" is a real answer
  for you, give it a tag such as `none`.

Aim for **at least 30–50 photos per tag**, taken the way your users will take them. A tag on almost
every photo (95% or more) teaches nothing and the screen warns about it; so does a table with more
than 100 tags. JPEG, PNG, BMP, GIF and WebP all work.

# Tag property photos

**Task:** Multi-label image classification — every tag that applies · **Data:** 268 property photos,
11 tags · **Time:** ~15 minutes · **Trains on:** GPU

An estate agency uploads dozens of photos with every new listing, and buyers search for what they
want: *a sea view*, *a pool*, *a garden*. Somebody has to tag every photo, and one photo is often
several things at once: a **bedroom** with a **balcony** and a **sea view**. A category can't say that:
a photo can only be in one folder.

[Leaf disease](../detect-leaf-disease/README.md) answers *which one*. This model answers **which
ones**: any number of tags per photo, including none.

## The data

| Path | What |
|---|---|
| `data/property-photos.zip` | 268 photos and `labels.csv`, the tags of each |
| `data/try/` | 6 more photos that are **not** in the ZIP, named after their tags |
| `data/CREDITS.md` | Title, author, licence and source of every photo |

```
property-photos.zip
├── labels.csv
└── photos/
    ├── photo-001.jpg
    └── … 268 photos
```

`labels.csv` holds the image's path inside the ZIP, then its tags separated by `;`:

```
image,labels
photos/photo-001.jpg,living room
photos/photo-007.jpg,outside;pool;garden
photos/photo-008.jpg,outside;pool;garden;balcony
photos/photo-012.jpg,kitchen;wooden floor
```

There are **11 tags**:
- **The room** (at most one): `kitchen`, `living room`, `bedroom`, `bathroom`, `outside`.
- **Features** (any number): `pool`, `sea view`, `garden`, `balcony`, `fireplace`, `wooden floor`.

A close-up of a fireplace or a floor has a feature tag and no room. Every tag is on 23–80 photos.

The photos are real, from [Wikimedia Commons](https://commons.wikimedia.org): public domain, CC0 or
CC BY only, resized to 500 px. They were chosen and tagged by hand for this example. Credits are in
[data/CREDITS.md](data/CREDITS.md), and the licences are summarised in [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. Sign in at [plaxorstack.com](https://plaxorstack.com) and open **Start training**.
2. Under *Computer Vision*, click **Multi-label Image Classification**.
3. Name it `Property tags` and drop in `data/property-photos.zip`. The screen reads `labels.csv` and
   shows **11 tags · 268 images**, with the photo count of each tag.
4. Click **Save and continue**.

## 2. Train

**Training** tab → **Start training**. This task always starts from **Pretrained (ResNet-50)**, a
model that already knows photos, so training takes well under a minute on a GPU.

The score is **Micro F1**, from 0 to 1: how well the predicted tags match the real ones, over all
tags together. Our test run scored **0.81**. That's good for real photos taken by many people in many
styles. Rooms are the easiest; small features like a fireplace in a corner are the hardest.

## 3. Try it

**Prediction** tab → **Run prediction**, and pick a photo from `data/try/`. The model answers with
every tag that fits, for example `outside · pool · garden`, or *No label applies* when nothing does.

## 4. Tag a whole listing at once

Add a **Results file** connector (Connectors → Add action application → Results file → PDF), and drag
it onto your model on the **AI Board**. Every batch sent through the API then writes one PDF with each
photo and its tags (or a ZIP with CSV and JSON). See [act on predictions](../../integrations/act-on-predictions/README.md) and
the developer docs for `POST /predict/image/batch`.

## Make it yours

Use your own listing photos and your own tags. A few dozen photos per tag is a good start. Tag every
photo with **all** the tags that fit: a missing tag teaches the model that the feature isn't there.

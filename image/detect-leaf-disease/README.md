# Detect leaf disease

**Task:** Image classification · **Data:** 252 field photos of bean leaves, 3 categories ·
**Time:** ~15 minutes · **Trains on:** GPU · **Also shows:** the chat widget with photo upload

Beans feed millions of smallholder farmers in East Africa, and two diseases, **angular leaf spot**
and **bean rust**, can destroy a crop if they aren't caught early. An agronomist can tell them apart
at a glance, but there aren't enough agronomists. A model that recognises them from a phone photo
puts that first opinion in every farmer's pocket.

The photos were taken in real fields in Uganda with ordinary phones: uneven light, soil and other
leaves in the background. They're the kind of pictures your users will actually send.

The same recipe recognises anything you can photograph: product defects on a line, damaged parcels,
which part a technician is holding, skin of fruit, waste for sorting.

## The data

| Path | What |
|---|---|
| `data/leaf-photos.zip` | 252 photos to train on, one folder per category |
| `data/try/` | 9 more photos — 3 per category — that are **not** in the ZIP, for testing |

```
leaf-photos.zip
├── angular_leaf_spot/   84 photos
├── bean_rust/           85 photos
└── healthy/             83 photos
```

The folder names are the categories. Photos were resized to 320 pixels on the longest side, which
keeps the ZIP to 8 MB. Source and licence: [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. **Machine Learning** → under *Computer Vision*, click **Image Classification**.
2. Name it `Leaf disease` and drop in `data/leaf-photos.zip`. The screen lists the three categories
   it found.
3. Click **Save and continue**.

There's nothing to map: an image ZIP needs only its folders.

## 2. Train

**Training** tab → **Start training**. The dialog offers **Pretrained (ResNet-50)**, already
selected, or *From scratch*. Keep the pretrained one. It has already learned to see edges, textures
and shapes from millions of photos, so 252 photos are enough to teach it three kinds of leaf. From
scratch would need thousands. Image models train on a GPU and fit within the free trial's monthly
GPU minutes.

**What to expect:** most held-out photos classified correctly. Angular leaf spot and bean rust
are the pair it confuses, since both show as spots. Healthy leaves are the easiest.

## 3. Try it in the browser

Open the **Prediction** tab, upload a photo from `data/try/`, and click **Run prediction**. The
file names tell you the right answer:

| Photo | Should be |
|---|---|
| `healthy-1.jpg` … `healthy-3.jpg` | `healthy` |
| `bean_rust-1.jpg` … `bean_rust-3.jpg` | `bean_rust` |
| `angular_leaf_spot-1.jpg` … `angular_leaf_spot-3.jpg` | `angular_leaf_spot` |

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                         # sends data/try/bean_rust-1.jpg
python predict.py ../data/try/healthy-2.jpg
```

Also `sh predict.sh [photo]`, `node predict.mjs [photo]`, `dotnet run predict.cs -- [photo]`.

```
bean_rust-1.jpg: bean_rust (score 0.97)
```

Images are sent as a multipart form, not JSON:

```sh
curl -X POST https://api.plaxorstack.com/predict/image \
  -H "X-Api-Key: $PLAXOR_API_KEY" \
  -F "trainingTypeId=$PLAXOR_MODEL_ID" \
  -F "image=@../data/try/bean_rust-1.jpg"
```

Read `predictedLabel`. To classify up to 16 photos in one call, send them as repeated `images` fields
to `/predict/image/batch`. You get one result per photo, in order.

## 5. Put it on a web page

`widget/index.html` is a "Leaf Doctor" page with the chat **inline**, beside the text rather than in
a corner, and with **photo upload** turned on. A visitor attaches a leaf photo and asks *"what's
wrong with this leaf?"*.

1. **AI Board** → choose this model → connect an LLM and turn chat on.
2. **Secrets** → create a **Widget** key for this model. Leave *Allowed sites* empty for now.
3. In `widget/index.html`, replace `YOUR_WIDGET_KEY`.
4. Serve the folder and open the address it prints:

   ```sh
   npx serve widget
   ```

```html
<div id="leaf-chat" style="height: 560px"></div>

<script src="https://plaxorstack.com/js/widget.js"
        data-key="YOUR_WIDGET_KEY"
        data-title="Leaf Doctor"
        data-mode="inline"
        data-target="#leaf-chat"
        data-image="true"
        defer></script>
```

Inline mode fills the element you point it at, so give that element a height.

## Make it yours

Put your photos in one folder per category, ZIP the folders, upload. Aim for **at least 30–50 photos
per category**, taken the way your users will take them — the same kind of phone, light and
background. Twenty studio shots teach a model less than fifty messy real ones. JPEG, PNG, BMP, GIF
and WebP all work.

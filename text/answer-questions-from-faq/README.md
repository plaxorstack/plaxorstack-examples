# Answer questions from your FAQ

**Task:** Semantic search · **Data:** 48 help-centre articles of a web shop · **Time:** ~5 minutes ·
**Trains on:** GPU (indexing only, nothing is fitted)

Customers never use your words. They type *"where is my parcel"*, and your help centre calls it
*"How can I track my order?"*. A keyword search finds nothing, because the two share no words. They
email support, and someone pastes the same link for the hundredth time this week.

**Semantic search** compares **meaning**, not words. A pretrained language model turns every
article and every question into a vector, and the articles closest in meaning come back first, with
a score. Nothing is learned from your data, so there's no labelling and no accuracy to wait for.
Upload the articles, build the index, search.

Put it in front of your support form ("did one of these answer your question?"), in your help
centre's search box, or behind a chatbot that answers from your own articles and hands over to a
person when nothing matches.

The same recipe finds anything by meaning: similar past tickets, products from a description,
clauses in contracts, the right internal wiki page.

## The data

`data/help-articles.csv`: one row per article.

| Column | Meaning |
|---|---|
| `id` | The article's id, unique. Returned with each match |
| `search_text` | The question and its answer in one column. **This is what gets searched** |
| `question` | The article's title |
| `answer` | The article's text |
| `link` | Where the article lives |

```
id,search_text,question,answer,link
ship-04,How can I track my order? You get an email with a tracking link ...,How can I track my order?,You get an email with a tracking link ...,https://shop.example.com/help/track-order
```

Articles cover delivery, returns, payment, accounts, orders, products and contacting support.

**Why `search_text` and not just the question?** Customers' words often match the **answer**
better than the title. *"Where is my parcel"* shares more meaning with *"you get an email with a
tracking link as soon as your parcel leaves…"* than with *"How can I track my order?"*. Indexing
both put the right article first for every test question below. With titles alone, several came
back with a near miss on top, such as the gift-returns article for *"can I send it back"*.

This dataset was written for the example: see [DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. **Machine Learning** → under *Natural Language Processing*, click **Semantic Search**.
2. Name it `Help centre` and drop in `data/help-articles.csv`.
3. Under **Map your fields**, choose `search_text` as **Text** and `id` as **Id**.
4. Click **Save and continue**.

**Every column you don't map comes back with each match**, here `question`, `answer` and `link`.
Your page can show the answer straight from the search result, with no second lookup.

## 2. Build the index

1. Open the **Training run** tab and click **Build index**.
2. Keep **MiniLM**, the default. DistilBERT is the other choice: larger and slower, sometimes better
   on long, technical documents.
3. Click **Start training**. 48 articles take seconds.

There's no accuracy figure: an index has nothing to be right or wrong about. The run reports how
many documents it holds.

**Adding articles later:** upload a file with the new or changed rows and build again. A row whose
`id` already exists **replaces** that article. Unchanged articles aren't encoded again, so a rebuild
is quick.

## 3. Try it in the browser

Open the **Prediction** tab, type a question, and click **Search**. These are the scores we measured
with MiniLM:

| Customer asks | Top match | Score |
|---|---|---|
| *where is my parcel* | How can I track my order? | 0.59 |
| *I forgot my password* | How do I reset my password? | 0.75 |
| *do you take paypal* | Which payment methods do you accept? | 0.54 |
| *my box came broken* | My order arrived damaged | 0.56 |
| *do you deliver to Canada* | Do you ship internationally? | 0.54 |
| *can I split the payment* | Can I pay in instalments? | 0.56 |
| *the jacket is too big, can I get a smaller one* | Can I exchange an item for a different size? | 0.45 |
| *how do I talk to a human* | How can I contact customer service or talk to a person? | 0.42 |
| *what's the weather like tomorrow* | *(nothing)* | best is 0.20, under the 0.30 cutoff |

**Reading the score:** cosine similarity, 0 to 1. With MiniLM, above **0.5** is a confident match,
**0.3–0.5** is probably relevant, and below **0.3** is unrelated. That's why 0.30 is the default
`minScore`.

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                              # "where is my parcel"
python predict.py "can I send it back"
```

Also `sh predict.sh ["question"]`, `node predict.mjs ["question"]`,
`dotnet run predict.cs -- ["question"]`.

```
> where is my parcel
  0.59  How can I track my order?
        https://shop.example.com/help/track-order
  0.56  My parcel says delivered but I haven't received it
        https://shop.example.com/help/missing-parcel
  0.51  How long does delivery take?
        https://shop.example.com/help/delivery-times
```

```json
POST https://api.plaxorstack.com/predict/text

{ "trainingTypeId": 1234, "text": "where is my parcel", "top": 3, "minScore": 0.3 }
```

```json
{
  "success": true,
  "matches": [
    {
      "id": "ship-04",
      "score": 0.585,
      "payload": {
        "question": "How can I track my order?",
        "answer": "You get an email with a tracking link as soon as your parcel leaves our warehouse. ...",
        "link": "https://shop.example.com/help/track-order"
      }
    }
  ]
}
```

| Field | Meaning |
|---|---|
| `top` | How many matches at most, 1–20. Default 5 |
| `minScore` | Leave out matches below this. Default 0.30 for both encoders |
| `matches` | Best first. **An empty list, not an error, when nothing is close enough** |
| `matches[].payload` | The unmapped columns of that row, as uploaded |

An empty `matches` is your cue to hand over to a person. To search for up to 32 questions in one
call, send `"texts": [ ... ]` to `/predict/text/batch`.

> Text models are used through the API. The chat widget answers questions about tabular and image
> models, not text ones.

## Make it yours

Export your help centre, product catalogue, or past tickets with their resolutions as a table:

- **An `id` column** that stays the same when an article changes, so re-uploading replaces it.
- **One text column to search.** Put everything a reader would match on into it: title and body,
  product name and description. Up to a few paragraphs works well. Very long documents are cut
  off, so split them by section, one row each.
- **Anything you want back** as extra columns: link, category, price, an image URL. At most 4 KB
  per row.

An index holds up to 200,000 documents. Under 10 works, but the page warns you: with so few,
a match is close only because there's little else to choose from.

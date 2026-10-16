# Filter spam messages

**Task:** Text classification · **Data:** 5,574 real SMS messages · **Time:** ~15 minutes ·
**Trains on:** GPU

Any product that lets people send messages — a contact form, a marketplace chat, comments, SMS —
gets spam. A text model trained on real messages learns what spam sounds like ("FREE", "claim your
prize", "txt WIN to…") and what an ordinary message sounds like, slang and typos included.

This example uses **real** text messages collected for research in the UK and Singapore. They're
informal, misspelled and full of abbreviations, which is exactly what real user-written text looks
like.

## The data

`data/sms-messages.csv`

| Column | Meaning |
|---|---|
| `message` | The text message, as sent |
| `category` | `spam` or `normal` — **what we predict** |

```
message,category
Ok lar... Joking wif u oni...,normal
Free entry in 2 a wkly comp to win FA Cup final tkts 21st May 2005. Text FA to 87121 to receive entry question(std txt rate)T&C's apply 08452810075over18's,spam
```

747 spam (13%) and 4,827 normal. Source and licence: [DATA-LICENSES.md](../../DATA-LICENSES.md).

> The messages are real, so a few are crude. That's the nature of spam.

## 1. Upload

1. **Machine Learning** → under *Natural Language Processing*, click **Text Classification**.
2. Name it `Spam filter` and drop in `data/sms-messages.csv`.
3. Under **Map your fields**, choose `message` as **Text** and `category` as **Label**.
4. Click **Save and continue**.

> Spam vs normal is a yes/no question, but it's still a **text** task. Free text goes to Text
> Classification, not Binary Classification.

## 2. Train

**Training run** tab → **Train model** → choose how to train → **Start training**. With 5,574
messages, *From scratch* does well and is the quickest. **MiniLM** copes better with slang it hasn't
seen. This set is larger than the ticket example, so it uses more of your monthly GPU minutes.

**What to expect:** around **98–99%** of held-out messages classified correctly. Watch the spam that
slips through rather than the overall figure: with only 13% spam, a model that let every spam
message through would still be 87% "accurate".

## 3. Try it in the browser

**Prediction** tab → paste a message → **Run prediction**:

| Message | Should be |
|---|---|
| *Congratulations! You have won a $1000 gift card. Reply WIN now to claim your prize.* | `spam` |
| *URGENT: your account will be suspended. Call 0800 123 456 to verify.* | `spam` |
| *Running 10 min late, save me a seat* | `normal` |
| *Can you pick up milk on the way home?* | `normal` |

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Category: spam
```

```json
POST https://api.plaxorstack.com/predict/text

{ "trainingTypeId": 1234, "text": "Congratulations! You have won a $1000 gift card. ..." }
```

Call it where messages arrive — before a comment is published, or a contact form is emailed — and
hold back what comes back as `spam` for review rather than deleting it outright.

## Make it yours

Collect messages your users or moderators have already marked. Two categories are enough, and more
work too (`spam`, `abuse`, `normal`). Text models are used through the API, not the chat widget.

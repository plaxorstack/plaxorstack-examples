# Route support tickets

**Task:** Text classification · **Data:** 320 customer tickets for 4 teams · **Time:** ~10 minutes ·
**Trains on:** GPU

A web shop's support inbox gets every kind of message — double charges, broken apps, locked accounts,
lost parcels — and someone reads each one just to forward it to the right team. A text model can do
that first read: it learns the **vocabulary** of each kind of problem, so a ticket that says
*"tracking says delivered but nothing came"* goes to shipping even though it never says "shipping".

The same recipe sorts any free text: emails by intent, reviews by topic, contracts by type, survey
answers by theme, chat messages by urgency.

## The data

The same tickets in both formats the platform accepts — use either:

| File | Layout |
|---|---|
| `data/tickets.csv` | A table: `ticket_text`, `team` |
| `data/tickets-by-team.zip` | One folder per team, one `.txt` file per ticket: `billing/ticket-001.txt` |

```
ticket_text,team
"Hi, Do you offer a receipt in PDF? I need it for expenses.",billing
"Good morning, Can I change the delivery address for order #83725? Please help.",shipping
```

| Team | Tickets | About |
|---|---|---|
| `billing` | 80 | charges, refunds, invoices, discount codes |
| `technical` | 80 | crashes, errors, slow pages, app updates |
| `account` | 80 | passwords, login, personal data, closing an account |
| `shipping` | 80 | late, lost, damaged or misdelivered parcels |

This dataset was written for the example: real support inboxes are private. See
[DATA-LICENSES.md](../../DATA-LICENSES.md).

## 1. Upload

1. **Machine Learning** → under *Natural Language Processing*, click **Text Classification**.
2. Name it `Ticket routing` and drop in **either** file:
   - `tickets.csv` — then, under **Map your fields**, choose `ticket_text` as **Text** and `team` as
     **Label**;
   - `tickets-by-team.zip` — nothing to map: the folder names are the categories.
3. Click **Save and continue**.

## 2. Train

1. On the training's page, open the **Training run** tab and click **Train model**.
2. Choose how to train. **MiniLM** suits a set this small: it's a pretrained encoder that already
   knows English, so 320 tickets are plenty. *From scratch* learns the words from your data alone,
   which works well once you have thousands of rows.
3. Click **Start training**.

Text models train on a GPU, so this uses a few of your monthly GPU minutes. A set this size takes a
couple of minutes.

**What to expect:** close to **100%** on held-out tickets. The four teams use very different words,
so this is an easy set. Real inboxes are messier: expect 85–95%, and look at the tickets it gets
wrong, which are often genuinely ambiguous.

## 3. Try it in the browser

Open the **Prediction** tab, paste a ticket it has never seen, and click **Run prediction**:

| Ticket | Should go to |
|---|---|
| *My parcel was marked as delivered yesterday but it is not here.* | `shipping` |
| *I was billed twice this month, please refund one of the payments.* | `billing` |
| *The app shows a white screen since I updated it.* | `technical` |
| *I never got the email to reset my password.* | `account` |

## 4. Call it from code

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
cd api
python predict.py                  # or: sh predict.sh · node predict.mjs · dotnet run predict.cs
```

```
Route to: shipping
```

Text models use their own endpoint:

```json
POST https://api.plaxorstack.com/predict/text

{ "trainingTypeId": 1234, "text": "Hello, my parcel was marked as delivered yesterday but it is not here." }
```

Read `predictedLabel`. In a real help desk you'd call this from the code that receives a new ticket,
and set its team or tag before a person ever opens it.

> Text models are used through the API. The chat widget answers questions about tabular and image
> models, not text ones.

## Make it yours

Export a few hundred handled tickets with the team that solved them. A CSV with a text column and a
category column is enough. Give every category **at least 5 examples**, and preferably 50 or more.
Long texts are fine (up to 200,000 characters per prediction).

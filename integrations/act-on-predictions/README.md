# Act on predictions

**Connects:** a prediction → a PDF, your endpoint, an app, or your own MCP server · **Works with:**
every model with a label · **Time:** ~15 minutes

A prediction on its own is an answer someone still has to act on. An **action** is the step after
it, attached to the model: *when the ticket is billing, post it to the finance system*; *when a
batch has urgent rows, write them into a PDF*; *when the user in the chat asks for a refund, offer
to open one*. You set it up once, on the AI Board, and it runs wherever the model is used: in its
chat, in the widget, from `/predict` and from batches. Your code doesn't change.

This recipe uses the ticket router from [Route support tickets](../../text/route-support-tickets/README.md).
Train it first (or use any model of yours with categories, and change the condition to match).

| Path | What |
|---|---|
| `data/overnight-tickets.txt` | 10 tickets, one per line, to send as a batch |
| `api/batch-results.{sh,py,mjs,cs}` | Send the batch, print each ticket's team, save the results file |
| `webhook/receiver.{py,mjs,cs}` | A small server that receives a Webhook action and checks its signature |

## How an action is built

Two parts: a **card**, which says *where*, and an **action** on a model, which says *when*.

**Cards** live under **Settings › Connectors**. Make one per destination, then reuse it on as many
models as you like:

| Card | Sends to | Needs |
|---|---|---|
| **Results file** | A PDF or ZIP of the matching rows, written by us, downloaded from a link | Nothing outside |
| **Webhook** | Your endpoint: `POST`, `PUT` or `PATCH` with the JSON body you write | A URL; an `Authorization` header if your endpoint wants one |
| **Composio** | An app operation through [Composio](https://composio.dev) — send a Gmail, post to Slack, create a HubSpot deal | Your Composio key and connected account |
| **MCP** | One tool on **your own** MCP server, with the arguments you fill in | The server's URL and how it signs in |

**Actions** are added on the **AI Board**: open the model, its actions, **New action**:

- **When** · **Is** · **Value** — the condition, on the
  prediction: `PredictedLabel` `Equal` `billing`, or `Probability` `Greater` `0.9`. **Is** also offers
  `NotEqual`, `Less`, `LessEqual`, `GreaterEqual`, `Contains` and `NotContains`; **When** `Always`
  runs on every prediction.
- **Application** — the card.
- **Body** (Webhook) or **Arguments** (Composio, MCP) — what is sent, with placeholders filled in
  from the prediction:

  | Placeholder | Becomes |
  |---|---|
  | `{{prediction.label}}` | The predicted label, e.g. `billing` |
  | `{{prediction.score}}`, `{{prediction.probability}}` | The number behind it |
  | `{{model.name}}` | The model's name on the board |
  | `{{timestamp}}` | When it ran, ISO 8601, UTC |
  | `{{inputs.YourColumn}}` | An input the prediction was made from — `{{inputs.Text}}` for a text model |
  | `{{arguments.name}}` | A value an AI fills in (below) |

  Every value is escaped for JSON, so a quote or a line break in a customer's text can't break your
  body. An unknown placeholder becomes empty rather than arriving as `{{typo}}`.
- **Before it runs** — **Ask first** (the chat shows your question with *Yes* / *No*, and nothing
  runs until Yes) or **Run immediately**. Only the chat can ask: from `/predict`, a batch or an MCP
  tool call there's nobody to say Yes, so only *Run immediately* actions run there.
- **Max runs per hour / per day** — a ceiling for this action alone. Every run also counts against
  your plan's action runs for the month.

## 1. A results file from a batch

The quickest one to try: nothing leaves PlaxorStack.

1. **Settings › Connectors** → new **Results file** card, file **PDF**, name it `Billing tickets PDF`.
2. **AI Board** → your ticket model → actions → **New action**:
   - When **PredictedLabel**, Is **Equal**, Value `billing`
   - Application: `Billing tickets PDF`
   - Before it runs: **Run immediately**
3. Run the batch:

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"      # Model # on the AI Board
cd api
python batch-results.py
```

Also `sh batch-results.sh`, `node batch-results.mjs`, `dotnet run batch-results.cs`.

```
billing    I was charged twice for order #58213, please refund one of the payments.
technical  The app closes as soon as I open my basket. I'm on Android 14.
shipping   Tracking says the parcel was delivered on Monday but nothing came.
billing    Can I get an invoice with my company's VAT number on it?
...

Saved results-1.pdf (31,204 bytes)
```

The PDF lists only the rows that met the condition, with each one's label and score. One batch with
a results file is **one action run**, however many rows it holds.

**How the script gets it.** A batch's answer is the usual array of predictions; the link comes in a
header, one per results-file action that matched at least one row:

```
X-Result-File: https://plaxorstack.com/action-results/Hk3…
```

The file is written a few seconds after the batch answers. Until then the link answers **202** with
`Retry-After: 5`; then **200** with the file. **410** means it couldn't be written, **404** that it
doesn't exist or has expired — links last **7 days**. The link is its own authorization: send no
key with it, and treat it like a password, since anyone holding it can download the file.

Text in any script comes out readable — Arabic right to left, Hindi, Thai, Chinese — so a batch of
multilingual tickets needs nothing special.

## 2. A webhook to your own system

1. **Settings › Connectors** → new **Webhook** card: method `POST`, URL
   `https://your-server.example.com/hooks/prediction`. Save, open it again, click **Show** under
   **Signing secret** and copy it.
2. On the ticket model, a new action: When **PredictedLabel**, Is **Equal**, Value `billing`, Application: your
   card, Before it runs: **Run immediately**, and a body such as

   ```json
   {
     "team": "{{prediction.label}}",
     "confidence": {{prediction.probability}},
     "ticket": "{{inputs.Text}}",
     "model": "{{model.name}}",
     "at": "{{timestamp}}"
   }
   ```

   (`confidence` has no quotes: it's a number.)
3. Start a receiver where that URL points:

```sh
export PLAXOR_WEBHOOK_SECRET="the signing secret"
cd webhook
python receiver.py          # or: node receiver.mjs, dotnet run receiver.cs
```

To try it from your own machine, expose port 8080 through a tunnel (ngrok, Cloudflare Tunnel) and
use the tunnel's HTTPS address as the card's URL. A webhook can't point at `localhost` or a private
address: we check the address just before every call and refuse those.

**What every delivery carries:**

| Header | What |
|---|---|
| `X-Training-Delivery-Id` | One id per delivery, **the same on every retry** — act on each id once |
| `X-Training-Timestamp` | Unix seconds when it was sent |
| `X-Training-Signature` | HMAC-SHA256, keyed with the signing secret, over `{timestamp}.{body}`, lowercase hex |
| `Authorization` | The card's header, if you set one |

To check a delivery, compute the HMAC over the timestamp header, a `.`, and the **raw** body bytes —
before parsing, since re-serialising JSON changes the bytes — and compare in constant time. Refuse a
timestamp more than five minutes old: it's signed, so an old delivery can't be replayed with a fresh
one. All three receivers do exactly this.

**What your answer means:**

| You answer | We do |
|---|---|
| 2xx | Delivered |
| 429 | Wait for your `Retry-After`, then try again. Not counted as a failure |
| 5xx, a timeout (15 s), no connection | Try again after 30 s, 1, 2, 4 min — five attempts in all |
| Any other 4xx, a redirect | Stop: the same request can't get a different answer. Give the final URL, not one that redirects |

After 10 failures in a row the action pauses for an hour, so a broken endpoint isn't hammered. The
action's history on the board shows every delivery: status, your response code and the first part of
your answer.

## 3. Ask first, in the chat

Change the action's **Before it runs** to **Ask first**. In the model's chat — on the board, or the
widget on your site — a ticket that lands in billing now comes back with *"Send billing to finance:
shall I go ahead?"*, the card it sends to, and **Yes** / **No**. Nothing is sent until Yes; an offer
nobody answers expires after 30 minutes.

**Message shown in the chat** is the reply after Yes, e.g. *"Sending this to finance now."* Say
"sending", not "sent": the delivery happens in the background a few seconds later.

The same action can be triggered by meaning as well as by the label:

- **When the user means…** — a *skill* you train under **AI Board › Skills** from a few example
  phrases per intent (*"I want my money back"*, *"refund please"*), in any language. Start it on
  **shadow: logged, never offered**: the action's history marks the turns it would have offered on,
  so you can see if it's right before switching it to **live**. Live always asks first.
- **When the chat's AI Brain proposes it** — with an LLM connected, the Brain may propose this
  action when the user asks in their own words. It can only pick from your actions, and always asks
  first.

**On a public widget** (no sign-in), an action is offered only if you tick **Also run for visitors of
the embedded widget** on it **and** allow it for that widget under **Secrets › Widgets › Public
actions**. It always asks first, the visitor passes a bot check before Yes runs it, runs are capped
per widget and per visitor, and one switch pauses them all.

## 4. Let an AI assistant propose an action

An assistant connected to your [MCP server](../README.md#use-your-models-from-an-ai-assistant-mcp)
can call your models. It can also **ask** for one of your actions to run — never run it. The request
waits on **Approvals** until the subscription's owner or an admin approves or declines it.

Two switches, both off by default:

1. **Secrets** → the assistant's key → **Allow proposals** (owner or admin only).
2. On the action → **Outside AIs may propose it through MCP**.

The assistant then sees a tool `propose_<name>_<id>`, taking:

- `reason` — why, shown to whoever approves;
- `arguments` — exactly the `{{arguments.…}}` names the action's body uses, every one required.

So an action an assistant may propose takes its values from `{{arguments.…}}`, e.g. a Webhook body:

```json
{ "customer": "{{arguments.customer_email}}", "amount": "{{arguments.amount}}", "why": "refund approved" }
```

An action that uses `{{inputs.…}}` or `{{prediction.…}}` can't be opened to proposals: an assistant
has no prediction to fill them with. The destination is fixed on the card, so no argument can change
where it goes, and the approver sees the body exactly as it will be sent.

Proposing costs nothing; an approval is one action run. A key can make 50 proposals an hour and
have 200 waiting; the same proposal twice is one; an unanswered proposal expires after 3 days.

## Things to know

- **Actions don't slow predictions down.** `/predict` answers first; deliveries are queued and sent
  by a worker within seconds.
- **The widget is a conversation, not a batch:** it never writes a results file.
- **Test without spending.** The model's prediction window on the board shows which actions *would*
  run, and runs none.
- **Personal data.** The delivery log keeps your endpoint's answer and the body sent for 30 days, so
  you can explain a failure; then it's deleted.

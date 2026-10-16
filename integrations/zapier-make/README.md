# Use a model from Zapier or Make, without code

**Use it for:** wiring a prediction into a workflow you already automate. A new form entry is
scored, a new email is classified, a new spreadsheet row gets a forecast, and the result goes to
Slack, a CRM or back into the sheet.

Both tools can call any HTTP API, so no PlaxorStack app is needed: one HTTP step with your key.

> The API key is stored inside your Zapier or Make account. Create a **separate key** just for it
> (*Secrets* → name it `zapier`), so you can delete that one alone if the account is ever
> compromised.

## The request

Whichever tool, the step is the same:

| | |
|---|---|
| Method | `POST` |
| URL | `https://api.plaxorstack.com/predict` for tabular, `/predict/text` for text |
| Header | `X-Api-Key`: your API key |
| Header | `Content-Type`: `application/json` |
| Body (tabular) | `{"trainingTypeId": 1234, "inputs": {"hour": "{{hour}}", "temperature_c": "{{temp}}"}}` |
| Body (text) | `{"trainingTypeId": 1234, "text": "{{email body}}"}` |

Map earlier steps' fields into the `{{…}}` places. In `inputs`, use **your own column names**, the
ones in the file you trained on. Read `predictedLabel` or `score` from the response
([which one](../../getting-started/README.md#what-comes-back)).

## Zapier

Example: classify every new Gmail message that reaches support, and post it to the right Slack
channel.

1. **Trigger:** Gmail → *New Email Matching Search* (`to:support@yourcompany.com`).
2. **Action:** **Webhooks by Zapier** → **Custom Request**.
   - Method `POST`, URL `https://api.plaxorstack.com/predict/text`.
   - Data: `{"trainingTypeId": 1234, "text": "` + *Body Plain* + `"}`.
   - Headers: `X-Api-Key` and `Content-Type: application/json`.

   Custom Request sends the data exactly as typed, so a quote or line break in the email breaks the
   JSON. Put a **Formatter → Text → Replace** step before it to escape `"`, or use the **POST**
   action instead: with *Payload Type* `json`, data fields `trainingTypeId` and `text`, Zapier builds
   the JSON safely. The POST action sends every value as text, including `"1234"`, which the API
   accepts.
3. **Action:** **Paths by Zapier** on `predictedLabel` (`billing`, `shipping`, …) → Slack
   *Send Channel Message* in each path.

Zapier's webhook steps need a paid Zapier plan.

## Make

Example: forecast demand for every new row in a Google Sheet and write it back.

1. **Google Sheets → Watch New Rows.**
2. **HTTP → Make a request.**
   - URL `https://api.plaxorstack.com/predict`, method `POST`.
   - Headers `X-Api-Key` and `Content-Type: application/json`.
   - Body type **Raw**, content type **JSON (application/json)**, request content
     `{"trainingTypeId": 1234, "inputs": {"hour": "{{1.hour}}", "temperature_c": "{{1.temperature}}"}}`.
   - **Parse response: Yes**, so later modules can map `score` and `predictedLabel` directly.
3. **Google Sheets → Update a Row**, writing `{{2.data.score}}` into the forecast column.

For free text in Make, wrap the field with `{{replace(1.text; "\""; "\\\"")}}`, or build the body
with a **JSON → Create JSON** module, which escapes it for you.

## Handling errors

- Turn on **error handling** for the HTTP step (Zapier: *autoreplay*; Make: an error handler route
  with *Break* to retry) for 429s and 5xx.
- A **429 with a message** means the month's predictions are used up, and retrying won't help.
  Route it to an email to yourself.
- Every run is one prediction. A Zap on a busy inbox can use the allowance quickly: add a filter
  step before the HTTP call so only the messages you care about are scored.

More in the [production guide](../../production/README.md).

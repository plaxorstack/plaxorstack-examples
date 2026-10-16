# Route Zendesk or Freshdesk tickets automatically

**Use it for:** setting the team on every new ticket the moment it arrives, using the model from
[Route support tickets](../../text/route-support-tickets/README.md) trained on your own tickets.

```
 customer ──► Zendesk / Freshdesk ──webhook {ticket_id}──► server.mjs ──► PlaxorStack /predict/text
                      ▲                                        │
                      └──────── set group + tags ◄─────────────┘
```

When the model is confident, the ticket is assigned to that team's group and tagged `ml_routed`.
When it isn't (below `MIN_CONFIDENCE`, 0.6 by default), the group is left alone and the ticket is
tagged `ml_unsure` plus `ml_guess_<label>`, so a person triages it and you can see what the model
would have done.

## 1. Train on your own tickets

Export a few hundred solved tickets with the group that handled them. Use subject + description as
the text and the group name as the label, then train as in
[Route support tickets](../../text/route-support-tickets/README.md). Note the labels it learned,
such as `billing` and `shipping`: you'll map each to a group id.

## 2. Run the server

It needs a public HTTPS address the help desk can reach: any small host (a container, a VM, Azure App
Service, Fly.io, Render), or `ngrok http 8080` while you try it.

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"
export GROUP_IDS='{"billing": 360001, "technical": 360002, "account": 360003, "shipping": 360004}'
export MIN_CONFIDENCE=0.6

node server.mjs          # Node 18+, no npm install needed
```

Then configure one help desk or both.

### Zendesk

```sh
export ZENDESK_SUBDOMAIN="yourcompany"            # yourcompany.zendesk.com
export ZENDESK_EMAIL="agent@yourcompany.com"
export ZENDESK_API_TOKEN="..."                    # Admin Center → Apps and integrations → Zendesk API
export ZENDESK_SIGNING_SECRET="..."               # from the webhook, step 2 below
```

1. **Group ids:** Admin Center → People → Groups. The id is in the group's URL.
2. **Webhook:** Admin Center → Apps and integrations → Webhooks → *Create webhook* → *Trigger or
   automation*. Endpoint `https://your-server/zendesk`, method POST, format JSON. After saving,
   reveal the **signing secret** and set `ZENDESK_SIGNING_SECRET`.
3. **Trigger:** Admin Center → Objects and rules → Triggers → *Add trigger*.
   - Conditions: **Ticket** *Is* **Created**.
   - Actions: **Notify by** → **Active webhook** → yours, with the body:

     ```json
     { "ticket_id": "{{ticket.id}}" }
     ```

The server checks Zendesk's signature on every call and refuses anything unsigned.

### Freshdesk

```sh
export FRESHDESK_DOMAIN="yourcompany"             # yourcompany.freshdesk.com
export FRESHDESK_API_KEY="..."                    # Profile settings → View API key
export FRESHDESK_SHARED_SECRET="$(openssl rand -hex 24)"
```

1. **Group ids:** Admin → Team → Groups. The id is in the group's URL.
2. **Automation:** Admin → Workflows → Automations → *Ticket creation* → *New rule*.
   - Condition: any ticket, or narrow it to a channel.
   - Action: **Trigger webhook**, POST, `https://your-server/freshdesk`.
     - Custom header `X-Webhook-Secret` set to your `FRESHDESK_SHARED_SECRET`.
     - Encoding JSON, content *Advanced*:

     ```json
     { "ticket_id": "{{ticket.id}}" }
     ```

Freshdesk doesn't sign webhooks, so the shared header is what stops anyone else posting to your
endpoint. Keep it secret like a password.

## Why it's built this way

- **The webhook carries only the ticket id.** The server reads the ticket through the help desk's
  API. Putting the customer's text into the webhook body works until someone writes a quote or a
  line break and the JSON breaks.
- **It answers 202 straight away** and does the work after. Help desks give webhooks a few seconds
  and retry on a timeout, which would classify the ticket twice.
- **Unsure tickets stay with people.** A wrong group is worse than no group: the ticket sits in the
  wrong queue. Start with a high `MIN_CONFIDENCE`, look at the `ml_unsure` tickets for a week, and
  lower it as you trust the model.
- **One prediction per new ticket.** 3,000 tickets a month is 3,000 predictions of your allowance.

## Measure it

After a couple of weeks, filter solved tickets by `ml_routed` and count how many were moved to
another group by an agent. That's your real accuracy. Retrain with the corrected tickets added
(*Add new data and train on everything*) to improve it.

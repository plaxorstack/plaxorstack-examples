# Call the API from your own server

**Shows:** how a web page uses a model without ever seeing your API key · **Languages:** Node
(Express) and C# (ASP.NET minimal API) · **Time:** ~5 minutes

> **Never put an API key in a web page, a mobile app or front-end JavaScript.** Anything the browser
> downloads, anyone can read with *View source*. A leaked API key can call **every** model of your
> subscription and spend your whole monthly allowance. Delete a leaked key at **Secrets** straight
> away and create a new one.

There are two safe ways to use a model from a web page:

| Want | Use |
|---|---|
| A chat window where visitors ask in plain language | The **widget** and a **Widget** key. It's built to be public: one model, only on the sites you list. See [getting-started §7](../getting-started/README.md#7-the-widget). |
| Your own form, button or page logic calling `/predict` | **This example.** Your page calls your server, and your server calls PlaxorStack with the key. |

```
 browser ──POST /api/route-ticket {text}──► your server ──X-Api-Key──► api.plaxorstack.com
         ◄──────────── {team} ─────────────            ◄── prediction ──
```

The page is a "Contact support" form that routes the message to a team, using the model from
[Route support tickets](../text/route-support-tickets/README.md). Train that one first, or point the
server at any text model.

## Run it

Both servers serve the same page, `public/index.html`, and expose the same endpoint.

```sh
export PLAXOR_API_KEY="your API key"
export PLAXOR_MODEL_ID="1234"

# Node 18+
cd express
npm install
npm start                       # http://localhost:3000

# or .NET 10
cd aspnet
dotnet run Program.cs           # opens on the address it prints
```

Type a problem and click **Send**: *"Thanks! Your message went to our shipping team."*

## What the server does, and why

| | Why |
|---|---|
| Reads the key from an environment variable | So it's never in a file you might commit. |
| Accepts only `{ text }` from the browser, 1–4,000 characters | Your endpoint is public. If it forwarded whatever it was sent, anyone could use it to call **any** of your models with your key. The model id is fixed on the server. |
| Limits each visitor to 10 requests a minute | Every prediction counts against your monthly allowance. Without a limit, one script can use it all up. Adjust to your traffic. |
| Returns only `{ team }` | The page needs nothing else. Fewer fields out, less to leak. |
| Logs PlaxorStack's error, shows the visitor a plain one | A 429 or 403 message is for you, not for your visitors. |
| Times out after 15 seconds | So a slow upstream doesn't hold your server's connections open. |

The in-memory limiter in `server.mjs` is per process. Behind a load balancer, use a shared store
(Redis, your gateway's rate limiting) instead. The ASP.NET one is the framework's own
`AddRateLimiter`, also per process.

## Make it yours

Change the route and the fields to fit your page: a price estimate from `/predict` with two inputs,
a photo check from `/predict/image`. Keep the same shape: **validate what comes in, fix the model id
on the server, return only what the page shows**. For handling 429s, retries and quotas, see the
[production guide](../production/README.md).

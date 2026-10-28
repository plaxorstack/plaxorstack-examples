# Integrations

Recipes for putting a trained model to work in the tools you already use. Each assumes you've
trained a model from one of the examples, or from your own data the same way.

| Recipe | Connects | Works with |
|---|---|---|
| [Batch-score a CSV](batch-score-csv/README.md) | A file → predictions as new columns, with retries and partial-failure handling | Tabular and text models |
| [Route help-desk tickets](helpdesk-routing/README.md) | Zendesk, Freshdesk → set the group on every new ticket | Text classification |
| [Add the widget to your site](website-widget/README.md) | Shopify, WordPress, Webflow, Squarespace, Wix | Tabular and image models |
| [Zapier and Make](zapier-make/README.md) | Any trigger → prediction → any app, no code | All models |
| [Call the API from your own server](../server-proxy/README.md) | Your web page → your back end → the model | All models |
| [Act on predictions](act-on-predictions/README.md) | A prediction → a PDF of the matching rows, your webhook, an app, your own MCP server; ask first in the chat | Every model with a label |
| [Use your models from an AI assistant](#use-your-models-from-an-ai-assistant-mcp) | claude.ai, ChatGPT, Claude Code, Claude Desktop, any MCP client → your models as tools | Tabular, text and semantic search models |

Before going live with any of them, read the [production guide](../production/README.md): keys,
retries, quotas and retraining.

## Use your models from an AI assistant (MCP)

`https://api.plaxorstack.com/mcp` is an [MCP](https://modelcontextprotocol.io) server (Streamable
HTTP). Every trained model on your AI Board becomes a tool the assistant can call:
`predict_<name>_<id>` for a tabular or text model, `search_<name>_<id>` for a semantic search index.
The tool's description is the one you wrote on the board, so write it for the assistant: what the
model decides and when to use it. Image models aren't offered.

- It takes a **sign-in** (claude.ai, ChatGPT, below) or an **API** key, in `X-Api-Key` or as
  `Authorization: Bearer <key>`. A widget key is refused.
- Each tool call counts as one prediction and shares the rate limit with `/predict`.
- The list is read live: a model you train later appears the next time the assistant lists tools.
- An assistant can also **propose** one of your actions, never run it: see
  [Act on predictions](act-on-predictions/README.md#4-let-an-ai-assistant-propose-an-action).

**claude.ai, Claude Desktop, ChatGPT — sign in, no key.** Add `https://api.plaxorstack.com/mcp` as a
custom connector — in claude.ai or Claude Desktop under *Settings › Connectors*, in ChatGPT as a
connector in developer mode — and leave the client id and secret empty. The assistant opens a
PlaxorStack page where you sign in and choose:

- **one subscription** — the connection sees that subscription's models and no other;
- what it may do — **call your models**, and, if you're the subscription's owner or an admin,
  **propose actions**. You can grant less than it asked for, never more.

What the connection holds works on `/mcp` only; it can never approve a proposal. It renews itself
while used, stops after 30 days unused or if you leave the subscription, and you can end it at any
time under **Settings › MCP › Connected AI apps**.

**Claude Code:**

```sh
claude mcp add --transport http plaxorstack https://api.plaxorstack.com/mcp \
  --header "X-Api-Key: $PLAXOR_API_KEY"
```

**Claude Desktop with a key** rather than a sign-in: its custom connector can't send one, so connect through
[`mcp-remote`](https://www.npmjs.com/package/mcp-remote) (needs Node.js). In
`claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "plaxorstack": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "https://api.plaxorstack.com/mcp", "--header", "Authorization:${PLAXOR_AUTH}"],
      "env": { "PLAXOR_AUTH": "Bearer paste-your-api-key" }
    }
  }
}
```

The header is passed through `env` without a space after the colon because Windows breaks
arguments at spaces.

**MCP Inspector**, to see the tools and try a call:

```sh
npx @modelcontextprotocol/inspector
```

Choose *Streamable HTTP*, enter the URL, and add the `X-Api-Key` header or the bearer token. Connect
through the Inspector's proxy, its default: the API sends no CORS headers, so a direct connection
from the browser is refused.

Both clients store the key in their own config file. Give the assistant **its own key**, so you can
delete it without breaking anything else.

# Add the widget to Shopify, WordPress or any site

**Use it for:** a chat on your shop or site where visitors ask your model questions in plain
language, such as *"how busy will the store be at 6 pm on Saturday?"* or *"what's wrong with this
leaf?"* with a photo.

The widget works with **tabular and image** models, anomaly detection included. Text models (text
classification, multi-label text, semantic search) are used through the API, for example with
[a server proxy](../../server-proxy/README.md).

## Before you start

1. **AI Board** → choose the model → connect an LLM and turn chat on.
2. **Secrets** → create a **Widget** key for that model.
3. Under *Allowed sites*, list **every address the page is served from**, exactly as the browser
   shows it: scheme, host, no path, no trailing slash. A missing one means the widget stays silent
   there.

   | Platform | List |
   |---|---|
   | Shopify | `https://yourstore.com`, `https://www.yourstore.com` **and** `https://yourstore.myshopify.com` (the theme editor's preview uses it) |
   | WordPress | `https://yoursite.com` and `https://www.yoursite.com`, plus your staging address if you have one |

The tag is the same everywhere:

```html
<script src="https://plaxorstack.com/js/widget.js"
        data-key="YOUR_WIDGET_KEY"
        data-title="Ask us"
        defer></script>
```

All its options (inline mode, photo upload, position) are in
[getting-started §7](../../getting-started/README.md#7-the-widget).

## Shopify

**On every page (floating button):**

1. Shopify admin → **Online Store** → **Themes** → your theme's **⋯** → **Edit code**.
2. Open `layout/theme.liquid`.
3. Paste the tag just before `</body>` and **Save**.

**On one page only, inline:** in the theme editor (**Customize**), add a **Custom Liquid** section
to that page's template and paste:

```html
<div id="ask-chat" style="height: 520px"></div>
<script src="https://plaxorstack.com/js/widget.js"
        data-key="YOUR_WIDGET_KEY"
        data-title="Ask us"
        data-mode="inline"
        data-target="#ask-chat"
        defer></script>
```

Changing themes later means pasting it again into the new one.

## WordPress

**On every page:** the simplest way is a header-and-footer plugin such as **WPCode** →
*Code Snippets* → *Header & Footer* → paste the tag into **Footer** → *Save*. This survives theme
updates. Editing the theme's `footer.php` directly doesn't: an update overwrites it.

**On one page, inline:** edit the page, add a **Custom HTML** block, and paste the inline version
above.

Caching and optimisation plugins (WP Rocket, Autoptimize, LiteSpeed) can delay or combine scripts.
If the widget doesn't appear, exclude `plaxorstack.com/js/widget.js` from their JavaScript
optimisation.

## Any other site

Paste the tag before `</body>` in your page template: Webflow (*Project settings → Custom code →
Footer*), Squarespace (*Settings → Advanced → Code injection → Footer*), Wix (*Settings → Custom code*,
placed in *Body – end*), or plain HTML.

## When it doesn't show up

A widget that's refused shows nothing on the page. Visitors shouldn't see an error about your
setup. Open the browser's developer console (F12) instead: every message starts with `[chat-widget]`.

| Console says, or you see | Check |
|---|---|
| The key was refused | The page's exact address is in *Allowed sites* (with and without `www`), and the key wasn't deleted. |
| `data-key is missing` | The `data-key` attribute is on the `<script>` tag itself. |
| `data-target did not match an element` | Inline mode: the `id` in `data-target="#…"` exists on the page. |
| Nothing in the console, nothing on the page | The script didn't load: a caching plugin, a content-security policy, or the page opened as `file://`. |
| The chat opens but can't answer | Chat is turned on and an LLM is connected for this model on the AI Board, and the month's chat messages aren't used up (Billing page). |

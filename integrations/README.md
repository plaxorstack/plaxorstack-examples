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

Before going live with any of them, read the [production guide](../production/README.md): keys,
retries, quotas and retraining.

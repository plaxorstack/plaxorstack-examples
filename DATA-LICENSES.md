# Data sources and licences

The **code** in this repository is MIT-licensed (see `LICENSE`). The **datasets** keep the licence of
their source. All of them allow commercial use and redistribution with attribution.

Every file under a sample's `data/` folder is produced by `tools/DatasetBuilder`, so you can see
exactly what was taken from the source and how it was changed:

```
dotnet run --project tools/DatasetBuilder
```

| Sample | Source | Licence | What we changed |
|---|---|---|---|
| `tabular/forecast-bike-rentals` | [Bike Sharing](https://archive.ics.uci.edu/dataset/275/bike+sharing+dataset) — Fanaee-T, H. (2013), UCI Machine Learning Repository. Capital Bikeshare, Washington D.C., 2011–2012 | CC BY 4.0 | Hourly file; kept hour, temperature (converted back to °C) and total rentals |
| `tabular/predict-online-purchase` | [Online Shoppers Purchasing Intention](https://archive.ics.uci.edu/dataset/468/online+shoppers+purchasing+intention+dataset) — Sakar, C. & Kastro, Y. (2018), UCI | CC BY 4.0 | Kept page value, visitor type and the purchase outcome (true/false); renamed visitor types |
| `tabular/grade-beans-by-variety` | [Dry Bean](https://archive.ics.uci.edu/dataset/602/dry+bean+dataset) — Koklu, M. & Ozkan, I. A. (2020), UCI | CC BY 4.0 | Kept length, width and variety |
| `tabular/segment-wholesale-customers` | [Wholesale Customers](https://archive.ics.uci.edu/dataset/292/wholesale+customers) — Cardoso, M. (2014), UCI | CC BY 4.0 | Kept annual fresh-food and grocery spend |
| `text/filter-spam-messages` | [SMS Spam Collection](https://archive.ics.uci.edu/dataset/228/sms+spam+collection) — Almeida, T. & Hidalgo, J. (2011), UCI | CC BY 4.0 | Converted to CSV; "ham" renamed "normal" |
| `image/detect-leaf-disease` | [Beans](https://huggingface.co/datasets/AI-Lab-Makerere/beans) — Makerere AI Lab (2020) | MIT | Validation + test splits only; resized to 320 px; 3 photos per class held out in `data/try/` |
| `tabular/recommend-products` | Written for this example (fixed seed) | MIT | — |
| `tabular/rank-hotel-search-results` | Written for this example (fixed seed) | MIT | — |
| `text/route-support-tickets` | Written for this example (fixed seed) | MIT | — |
| `tabular/spot-unusual-server-load` | Written for this example (fixed seed): office-hours traffic with 11 planted incidents | MIT | — |
| `text/tag-product-reviews` | Written for this example (fixed seed) | MIT | — |
| `text/answer-questions-from-faq` | Written for this example | MIT | — |
| `tabular/forecast-bakery-sales` | Written for this example (fixed seed): three shops' weekly rhythm, seasons, growth and planned promotions | MIT | — |
| `image/tag-fabric-swatches` | Drawn for this example (fixed seed): striped, dotted, checked and plain swatches, rotated, lit unevenly and grained; 6 held out in `data/try/` | MIT | — |

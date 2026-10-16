# Going to production

The examples call a model once and print the answer. Real code calls it thousands of times, from a
server, for months. This page covers what changes.

## Keys

- **API keys live on your server only**: an environment variable or your platform's secret store
  (Azure Key Vault, AWS Secrets Manager, GitHub Actions secrets, Docker secrets). Never in a web
  page, a mobile app, a config file you commit, or a log line. To call a model from your own web
  page, see [server-proxy](../server-proxy/README.md).
- **One key per environment and per application.** Separate keys for staging and production, and
  for the nightly batch job and the website. When one leaks you delete that one, and the others keep
  working.
- **Rotate without downtime:**
  1. Create a new key at **Secrets**.
  2. Deploy it: update the environment variable and restart.
  3. Check that requests succeed with the new key.
  4. Delete the old key.

  Rotate on a schedule (every 90 days is common), whenever someone with access leaves, and
  immediately if a key may have been exposed.
- **Widget keys** are public by design. Before going live, fill in *Allowed sites* with your exact
  origins (`https://www.example.com`, and the version without `www` if people use it). An empty list
  means any site can use the key.

## Errors and retries

| Status | Retry? | What to do |
|---|---|---|
| 400 | No | The request is wrong, or the model isn't trained. Read `message`, fix it, and log it. |
| 401 | No | The key doesn't exist: deleted, mistyped, or the variable is empty. Alert someone. |
| 403 | No | A widget key used outside its model or site, or a plan limit. |
| 410 | No | The training was retired. Point your code at another model. |
| 429, **empty body** | **Yes** | Too many requests this minute. Wait and try again. |
| 429, with `message` | No | The month's predictions are used up. Retrying won't help until the 1st, or until you upgrade. |
| 500, 502, 503, 504, timeout | Yes | Temporary. Retry a few times with a growing delay. |

**Rate limits.** Each API key can make **120 prediction requests a minute** and **20 chat requests a
minute**. The window resets every minute, so after a 429 with no body, waiting 5–10 seconds and then
backing off further is enough. There's no `Retry-After` header. A **batch call counts as one
request** however many rows it carries, so for volume, batch (see below) rather than calling faster.

A retry with backoff, in Python:

```python
import random, time, requests

RETRY_STATUS = {429, 500, 502, 503, 504}

def predict(session, url, payload, attempts=5):
    for attempt in range(attempts):
        try:
            response = session.post(url, json=payload, timeout=30)
        except (requests.ConnectionError, requests.Timeout):
            response = None

        if response is not None:
            # A 429 with a body is the monthly quota: waiting won't fix it.
            quota_exhausted = response.status_code == 429 and response.content
            if response.status_code not in RETRY_STATUS or quota_exhausted:
                return response

        if attempt < attempts - 1:
            time.sleep(min(60, 2 ** attempt * 2) + random.uniform(0, 1))   # 2, 4, 8, 16 s + jitter
    return response
```

And in C#, with [Polly](https://www.pollydocs.org/) through `Microsoft.Extensions.Http.Resilience`:

```csharp
builder.Services.AddHttpClient("plaxor", http =>
{
    http.BaseAddress = new Uri("https://api.plaxorstack.com");
    http.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["PLAXOR_API_KEY"]);
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 4;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
    // Don't retry the monthly quota: it's a 429 too, but with a JSON body.
    options.Retry.ShouldHandle = args => ValueTask.FromResult(
        args.Outcome.Exception is HttpRequestException or TimeoutException ||
        args.Outcome.Result is { StatusCode: >= HttpStatusCode.InternalServerError } ||
        args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests, Content.Headers.ContentLength: 0 });
});
```

**Always set a timeout** (the examples use 30 seconds) so a slow response doesn't hold your request
threads. **Never retry inside a user's request for long:** a page that waits 30 seconds to say "try
again" is worse than one that says it straight away.

## Batches and partial failures

For more than a handful of predictions, send them together:

| | Endpoint | Per call |
|---|---|---|
| Tabular rows | `/predict/batch` | 1,000 rows |
| Texts | `/predict/text/batch` | 32 texts, 200,000 characters in all |
| Images | `/predict/image/batch` | 16 images |

A batch answers with **an array, one result per item, in the order sent**. The HTTP status is 200
even when some items failed, so **check `success` on every item**:

```json
[
  { "success": true,  "score": 512.4, ... },
  { "success": true,  "score": 98.1,  ... },
  { "success": false, "message": "The monthly prediction limit has been reached." }
]
```

Each item counts as one prediction. If your monthly allowance runs out partway through, the items
before that point succeed and the rest come back like the last one above. Keep the rows that failed
and resume them next month or after an upgrade; don't resend the whole batch.
[Batch-score a CSV](../integrations/batch-score-csv/README.md) does exactly this.

## Quotas

Your plan sets how many predictions, chat messages and GPU minutes a subscription gets per month,
**shared by every member and every key** of the subscription. They reset on the 1st. Models held
and storage don't reset: they're what you keep.

- Watch usage on the **AI Board** and the **Billing** page. Set yourself a reminder at 80%.
- A batch job that runs every night will use the allowance faster than a website. Estimate:
  rows per night × 30.
- Cache predictions you can. The same inputs give the same output until you retrain, so a product
  page doesn't need a fresh prediction on every view.

## Retraining

Data changes: new products, new kinds of ticket, a new season. A model trained once slowly goes
stale.

- **Retraining keeps the model id.** When you train an existing training again, it asks what to
  train on:
  - *Train again on the same data*,
  - *Add new data and train on everything*,
  - *Train on the new data only*.

  The new file must have the same columns as the first. Your code carries on calling
  `trainingTypeId: 1234` unchanged, and the new version takes over when it finishes, so there's no
  moment with no model.
- **You can go back.** The training page keeps the **previous model** and can restore it if the
  new one turns out worse.
- **Retrain on a schedule** that matches how fast your data moves:
  - monthly for demand and prices;
  - whenever you add categories, for text and images;
  - after every help-centre update, for a search index. A rebuild re-encodes only the rows that
    changed.
- **Prefer "add new data" to "new data only"** unless the old data is now wrong. More history
  usually means a steadier model.
- **Compare before you trust it.** Note the metric after each training (next section). If a
  retrain is clearly worse, restore the previous model and look at what changed in the data.

## Is the model good enough?

Each training shows its score on held-out rows, data it didn't see while learning. What "good"
means depends on the task:

| Task | Metric | Read it as |
|---|---|---|
| Regression | R² and mean absolute error | R² 1.0 is perfect, 0 is no better than always guessing the average. The error is in your units ("off by 40 bikes on average"). |
| Binary | Accuracy, AUC, F1 | With rare positives, accuracy misleads (see below). Look at AUC and F1. |
| Multiclass, text, image | Accuracy, per-category scores | Check the weakest category, not only the average. |
| Multi-label text | Micro F1 | Above 0.8 is good on real data. |
| Clustering | Average distance | Lower is tighter. Judge by whether the groups make sense to you. |
| Ranking | NDCG | 1.0 means the best items always came first. |
| Anomaly detection | Average anomaly score | Lower is better. About 1% of training rows are flagged by design. |
| Semantic search | None | Try real questions and read the scores (above 0.5 is confident with MiniLM). |

Three habits matter more than the number:

1. **Beat the obvious guess.** If 95% of sessions don't buy, a model that always says "no" is 95%
   accurate and useless. Compare with the most common answer.
2. **Try it on cases you know.** Ten real inputs with answers you're sure of tell you more than a
   percentage. Every example lists some.
3. **Look at the mistakes.** Ambiguous tickets, mislabelled rows and blurry photos are usually the
   cause, and fixing the data helps more than anything else.

On real data, expect lower scores than on the examples: they were chosen to train cleanly.

## Checklist

- [ ] API key in a secret store, one per environment, rotation planned
- [ ] Widget *Allowed sites* filled in
- [ ] Timeout on every call; retries only on empty-body 429, 5xx and timeouts
- [ ] Batches check `success` per item and keep the failed rows
- [ ] Input validated before sending (a non-number where a number is expected is read as 0)
- [ ] Usage watched against the plan; caching where inputs repeat
- [ ] Retraining scheduled; the metric noted after each run

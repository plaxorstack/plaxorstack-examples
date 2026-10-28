// Asks your model for a shop's next 14 days of sales.
// Usage: node predict.mjs [shop] [promotion days]   e.g. node predict.mjs station-kiosk 5,12
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const horizon = 14;
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;
const [shop = "harbour-street", promotionDays = ""] = process.argv.slice(2);

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

// One [on_promotion] per day ahead: the model was trained with it, so it needs the plan.
const promotions = new Set(promotionDays.split(",").filter(Boolean).map(Number));
const covariates = Array.from({ length: horizon }, (_, i) => [promotions.has(i + 1) ? 1 : 0]);

const response = await fetch(`${apiUrl}/predict/forecast`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ trainingTypeId: Number(modelId), horizon, series: shop, covariates }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

const { forecast } = result;
console.log(`${forecast.series}, ${forecast.frequency} — next ${forecast.points.length} days`);

for (const point of forecast.points) {
    const day = point.date.slice(0, 10);
    const weekday = new Date(`${day}T00:00:00Z`).toLocaleDateString("en-GB", { weekday: "short", timeZone: "UTC" });
    const value = Math.round(point.value).toString().padStart(5);
    console.log(`${day}  ${weekday}  ${value} loaves   likely ${Math.round(point.lower)}–${Math.round(point.upper)}`);
}

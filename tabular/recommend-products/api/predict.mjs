// Asks your model the products a customer is most likely to rate highly.
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const response = await fetch(`${apiUrl}/predict`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ trainingTypeId: Number(modelId), inputs: { customer_id: "C0137" } }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

for (const item of result.recommendations.slice(0, 5))
    console.log(`${item.score.toFixed(1)}  ${item.item}`);

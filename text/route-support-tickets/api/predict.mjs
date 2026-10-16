// Asks your model which team should handle a support ticket.
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const response = await fetch(`${apiUrl}/predict/text`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ trainingTypeId: Number(modelId), text: "Hello, my parcel was marked as delivered yesterday but it is not here. The courier is DHL." }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

console.log(`Route to: ${result.predictedLabel}`);

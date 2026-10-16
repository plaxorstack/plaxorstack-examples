// Asks your model whether a server reading is unusual.
// Usage: node predict.mjs [requests_per_min] [cpu_percent]   (default: 250 requests, 86% CPU)
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;
const [requestsPerMin = "250", cpuPercent = "86"] = process.argv.slice(2);

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const response = await fetch(`${apiUrl}/predict`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({
        trainingTypeId: Number(modelId),
        inputs: { requests_per_min: requestsPerMin, cpu_percent: cpuPercent },
    }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

const unusual = result.predictedLabel === "True" ? "yes" : "no";
console.log(`${requestsPerMin} requests/min at ${cpuPercent}% CPU — unusual: ${unusual} (score ${result.score.toFixed(2)})`);

// Searches your help articles for the ones that answer a customer's question.
// Usage: node predict.mjs ["your question"]   (default: "where is my parcel")
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;
const question = process.argv[2] ?? "where is my parcel";

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const response = await fetch(`${apiUrl}/predict/text`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ trainingTypeId: Number(modelId), text: question, top: 3, minScore: 0.3 }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

console.log(`> ${question}`);

// Empty, not an error, when nothing is close enough: hand over to a person.
if (result.matches.length === 0) console.log("  No article answers that. Passing you to our team.");

for (const match of result.matches) {
    const payload = match.payload ?? {};
    console.log(`  ${match.score.toFixed(2)}  ${payload.question ?? match.id}`);
    console.log(`        ${payload.link ?? ""}`);
}

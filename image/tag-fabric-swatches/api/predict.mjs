// Sends a photo of a fabric swatch to your model and prints every tag it gives it.
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
//
//   node predict.mjs                         # a held-out photo from ../data/try
//   node predict.mjs path/to/your-swatch.jpg   # your own photo
import { readFile } from "node:fs/promises";
import { basename } from "node:path";
import { fileURLToPath } from "node:url";

const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const photo = process.argv[2] ?? fileURLToPath(new URL("../data/try/red-white-striped.jpg", import.meta.url));

const form = new FormData();
form.append("trainingTypeId", modelId);
form.append("image", new Blob([await readFile(photo)], { type: "image/jpeg" }), basename(photo));

// No Content-Type header: fetch sets multipart/form-data with its boundary itself.
const response = await fetch(`${apiUrl}/predict/image`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey },
    body: form,
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const result = body ? JSON.parse(body) : {};

if (!response.ok || !result.success) {
    console.error(`${response.status}: ${result.message ?? response.statusText}`);
    process.exit(1);
}

// Every tag likely enough, each against its own threshold. Empty when none is.
const tags = result.predictedLabels ?? [];
console.log(`${basename(photo)}: ${tags.length ? tags.join(", ") : "(no tags)"}`);

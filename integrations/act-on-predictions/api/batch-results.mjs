// Sends a night's tickets to your model in one batch and saves the results file its action writes.
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
//
//   node batch-results.mjs                      # ../data/overnight-tickets.txt, one ticket per line
//   node batch-results.mjs path/to/tickets.txt
import { readFile, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { setTimeout as sleep } from "node:timers/promises";

const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const path = process.argv[2] ?? fileURLToPath(new URL("../data/overnight-tickets.txt", import.meta.url));
const tickets = (await readFile(path, "utf8"))
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .slice(0, 32); // a batch holds at most 32 texts

const response = await fetch(`${apiUrl}/predict/text/batch`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ trainingTypeId: Number(modelId), texts: tickets }),
});

// A rejected key (401) has no body, so don't assume JSON.
const body = await response.text();
const results = body ? JSON.parse(body) : {};

if (!response.ok) {
    console.error(`${response.status}: ${results.message ?? response.statusText}`);
    process.exit(1);
}

results.forEach((result, i) => {
    const label = result.success ? result.predictedLabel : `failed: ${result.message}`;
    console.log(`${label.padEnd(10)} ${tickets[i].slice(0, 70)}`);
});

// One header per Results file action that matched at least one row; fetch joins repeats with ", ".
// None: no such action on this model, it is set to Ask first, or no row met its condition.
const links = (response.headers.get("X-Result-File") ?? "").split(", ").filter(Boolean);

if (links.length === 0) {
    console.log("\nNo results file: no Run immediately action matched any row.");
    process.exit(0);
}

for (const [i, link] of links.entries()) {
    // The worker writes the file a few seconds after the batch answers. Until then: 202 + Retry-After.
    // The link is its own authorization, so the API key is not sent with it.
    let download;
    for (let attempt = 0; attempt < 24; attempt++) {
        download = await fetch(link);
        if (download.status !== 202) break;
        await sleep(Number(download.headers.get("Retry-After") ?? 5) * 1000);
    }

    if (download.status !== 200) {
        console.error(`Results file ${i + 1}: ${download.status} ${(await download.text()).slice(0, 200)}`);
        process.exit(1);
    }

    const bytes = Buffer.from(await download.arrayBuffer());
    const name = `results-${i + 1}.${(download.headers.get("Content-Type") ?? "").includes("zip") ? "zip" : "pdf"}`;
    await writeFile(name, bytes);
    console.log(`\nSaved ${name} (${bytes.length.toLocaleString()} bytes)`);
}

// A small back end that keeps your PlaxorStack API key on the server.
// The browser calls POST /api/route-ticket with { text }; this server adds the key and the model id
// and forwards the request. Run: npm install && npm start, then open http://localhost:3000
import express from "express";
import { fileURLToPath } from "node:url";

const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = Number(process.env.PLAXOR_MODEL_ID);
const port = Number(process.env.PORT ?? 3000);

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

const app = express();
app.use(express.json({ limit: "16kb" }));
app.use(express.static(fileURLToPath(new URL("../public", import.meta.url))));

// Every prediction counts against YOUR monthly allowance, so a public endpoint needs a limit of its
// own. In memory and per process: enough for one server; use a shared store behind a load balancer.
const windowMs = 60_000;
const maxPerWindow = 10;
const hits = new Map();

function allowed(ip) {
    const now = Date.now();
    const recent = (hits.get(ip) ?? []).filter((t) => now - t < windowMs);
    recent.push(now);
    hits.set(ip, recent);
    return recent.length <= maxPerWindow;
}

app.post("/api/route-ticket", async (req, res) => {
    if (!allowed(req.ip)) return res.status(429).json({ error: "Too many requests, try again in a minute." });

    // Accept only what this page needs. Never forward a model id or arbitrary JSON from the browser:
    // anyone can call this endpoint, not only your page.
    const text = typeof req.body?.text === "string" ? req.body.text.trim() : "";
    if (!text || text.length > 4000) return res.status(400).json({ error: "Write between 1 and 4,000 characters." });

    try {
        const response = await fetch(`${apiUrl}/predict/text`, {
            method: "POST",
            headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
            body: JSON.stringify({ trainingTypeId: modelId, text }),
            signal: AbortSignal.timeout(15_000),
        });
        const body = await response.text();
        const result = body ? JSON.parse(body) : {};

        if (!response.ok || !result.success) {
            // Log the details for yourself; tell the visitor only that it failed.
            console.error(`PlaxorStack ${response.status}: ${result.message ?? response.statusText}`);
            return res.status(502).json({ error: "The prediction service is unavailable." });
        }

        // Return only what the page shows, not the whole response.
        res.json({ team: result.predictedLabel });
    } catch (error) {
        console.error("PlaxorStack request failed:", error.message);
        res.status(502).json({ error: "The prediction service is unavailable." });
    }
});

app.listen(port, () => console.log(`Open http://localhost:${port}`));

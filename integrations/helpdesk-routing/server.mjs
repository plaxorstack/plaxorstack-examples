// Routes every new help-desk ticket to a team with your text model, before anyone opens it.
// Zendesk and Freshdesk call this server when a ticket is created; it asks PlaxorStack which team
// should handle it and sets the ticket's group, or tags it for a person when the model isn't sure.
// Node 18+, no dependencies. Configuration: see README.md.
import { createServer } from "node:http";
import { createHmac, timingSafeEqual } from "node:crypto";

const env = (name, fallback) => {
    const value = process.env[name] ?? fallback;
    if (value === undefined) {
        console.error(`Set ${name} first (see README.md).`);
        process.exit(1);
    }
    return value;
};

const plaxor = {
    url: env("PLAXOR_API_URL", "https://api.plaxorstack.com"),
    key: env("PLAXOR_API_KEY"),
    modelId: Number(env("PLAXOR_MODEL_ID")),
};
// Which help-desk group each label goes to, e.g. {"billing": 360001, "shipping": 360002}.
const groups = JSON.parse(env("GROUP_IDS"));
// Below this confidence the ticket is left for a person, tagged "ml_unsure".
const minConfidence = Number(env("MIN_CONFIDENCE", "0.6"));
const port = Number(env("PORT", "8080"));

async function classify(text) {
    const response = await fetch(`${plaxor.url}/predict/text`, {
        method: "POST",
        headers: { "X-Api-Key": plaxor.key, "Content-Type": "application/json" },
        body: JSON.stringify({ trainingTypeId: plaxor.modelId, text }),
        signal: AbortSignal.timeout(15_000),
    });
    const body = await response.text();
    const result = body ? JSON.parse(body) : {};
    if (!response.ok || !result.success) throw new Error(`PlaxorStack ${response.status}: ${result.message ?? response.statusText}`);
    // For a text model, score is the winning label's confidence, 0 to 1.
    return { label: result.predictedLabel, confidence: result.score ?? 0 };
}

// Decides group and tags. A label with no group configured is treated as unsure, not dropped.
function decide({ label, confidence }) {
    const groupId = groups[label];
    if (groupId === undefined || confidence < minConfidence)
        return { groupId: undefined, tags: ["ml_unsure", `ml_guess_${label}`] };
    return { groupId, tags: ["ml_routed", `ml_${label}`] };
}

// ---- Zendesk -------------------------------------------------------------------------------------

const zendesk = {
    subdomain: process.env.ZENDESK_SUBDOMAIN,
    email: process.env.ZENDESK_EMAIL,
    token: process.env.ZENDESK_API_TOKEN,
    signingSecret: process.env.ZENDESK_SIGNING_SECRET,
};

async function zendeskApi(method, path, body) {
    const auth = Buffer.from(`${zendesk.email}/token:${zendesk.token}`).toString("base64");
    const response = await fetch(`https://${zendesk.subdomain}.zendesk.com/api/v2${path}`, {
        method,
        headers: { Authorization: `Basic ${auth}`, "Content-Type": "application/json" },
        body: body && JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`Zendesk ${method} ${path}: ${response.status} ${await response.text()}`);
    return response.json();
}

// Zendesk signs each webhook: base64(HMAC-SHA256(secret, timestamp + body)).
function zendeskSignatureValid(headers, rawBody) {
    const signature = headers["x-zendesk-webhook-signature"];
    const timestamp = headers["x-zendesk-webhook-signature-timestamp"];
    if (!signature || !timestamp) return false;
    const expected = createHmac("sha256", zendesk.signingSecret).update(timestamp + rawBody).digest("base64");
    return safeEqual(signature, expected);
}

async function routeZendesk(payload) {
    // The webhook sends only the id. The ticket is read through the API, so a quote or newline in
    // the customer's text can't break the webhook's JSON.
    const { ticket } = await zendeskApi("GET", `/tickets/${encodeURIComponent(payload.ticket_id)}.json`);
    const decision = decide(await classify(`${ticket.subject}\n\n${ticket.description}`));
    await zendeskApi("PUT", `/tickets/${ticket.id}.json`, {
        ticket: { ...(decision.groupId && { group_id: decision.groupId }), additional_tags: decision.tags },
    });
    return { ticket: ticket.id, ...decision };
}

// ---- Freshdesk -----------------------------------------------------------------------------------

const freshdesk = {
    domain: process.env.FRESHDESK_DOMAIN,
    apiKey: process.env.FRESHDESK_API_KEY,
    // Freshdesk doesn't sign webhooks, so the automation rule sends this in a header instead.
    sharedSecret: process.env.FRESHDESK_SHARED_SECRET,
};

async function freshdeskApi(method, path, body) {
    const auth = Buffer.from(`${freshdesk.apiKey}:X`).toString("base64");
    const response = await fetch(`https://${freshdesk.domain}.freshdesk.com/api/v2${path}`, {
        method,
        headers: { Authorization: `Basic ${auth}`, "Content-Type": "application/json" },
        body: body && JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`Freshdesk ${method} ${path}: ${response.status} ${await response.text()}`);
    return response.json();
}

async function routeFreshdesk(payload) {
    const id = encodeURIComponent(payload.ticket_id);
    const ticket = await freshdeskApi("GET", `/tickets/${id}`);
    const decision = decide(await classify(`${ticket.subject}\n\n${ticket.description_text}`));
    // Freshdesk replaces the tag list on update, so keep the ones the ticket already has.
    await freshdeskApi("PUT", `/tickets/${id}`, {
        ...(decision.groupId && { group_id: decision.groupId }),
        tags: [...new Set([...(ticket.tags ?? []), ...decision.tags])],
    });
    return { ticket: ticket.id, ...decision };
}

// ---- HTTP ----------------------------------------------------------------------------------------

function safeEqual(a, b) {
    const left = Buffer.from(String(a));
    const right = Buffer.from(String(b));
    return left.length === right.length && timingSafeEqual(left, right);
}

function readBody(request, limit = 64 * 1024) {
    return new Promise((resolve, reject) => {
        let size = 0;
        const chunks = [];
        request.on("data", (chunk) => {
            size += chunk.length;
            if (size > limit) {
                reject(new Error("body too large"));
                request.destroy();
            } else chunks.push(chunk);
        });
        request.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
        request.on("error", reject);
    });
}

const routes = {
    "/zendesk": {
        enabled: () => zendesk.subdomain && zendesk.email && zendesk.token && zendesk.signingSecret,
        authorised: (request, raw) => zendeskSignatureValid(request.headers, raw),
        handle: routeZendesk,
    },
    "/freshdesk": {
        enabled: () => freshdesk.domain && freshdesk.apiKey && freshdesk.sharedSecret,
        authorised: (request) => safeEqual(request.headers["x-webhook-secret"] ?? "", freshdesk.sharedSecret),
        handle: routeFreshdesk,
    },
};

createServer(async (request, response) => {
    const route = routes[request.url];
    if (request.method !== "POST" || !route?.enabled()) {
        response.writeHead(404).end();
        return;
    }
    try {
        const raw = await readBody(request);
        if (!route.authorised(request, raw)) {
            response.writeHead(401).end();
            return;
        }
        // Answer at once: help desks time webhooks out after a few seconds and retry, and a retry
        // would classify the same ticket twice.
        response.writeHead(202).end();
        const outcome = await route.handle(JSON.parse(raw));
        console.log(`${request.url} ticket ${outcome.ticket}: group ${outcome.groupId ?? "unchanged"}, tags ${outcome.tags.join(" ")}`);
    } catch (error) {
        console.error(`${request.url}: ${error.message}`);
        if (!response.headersSent) response.writeHead(400).end();
    }
}).listen(port, () => {
    const enabled = Object.keys(routes).filter((path) => routes[path].enabled());
    console.log(`Listening on :${port} for ${enabled.join(", ") || "nothing: configure Zendesk or Freshdesk"}`);
});

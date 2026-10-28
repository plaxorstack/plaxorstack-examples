// Receives a Webhook action's deliveries and checks each one really came from PlaxorStack.
// Needs Node 18 or later, no packages. Set PLAXOR_WEBHOOK_SECRET to the card's signing secret first.
//
//   node receiver.mjs            # listens on http://localhost:8080/hooks/prediction
import { createHmac, timingSafeEqual } from "node:crypto";
import { createServer } from "node:http";

const secret = process.env.PLAXOR_WEBHOOK_SECRET;
const port = Number(process.env.PORT ?? 8080);

if (!secret) {
    console.error("Set PLAXOR_WEBHOOK_SECRET first.");
    process.exit(1);
}

// Every retry of one delivery carries the same id. Keep these in your database in production: this
// set is forgotten when the process restarts.
const seen = new Set();

function isGenuine(timestamp, body, signature) {
    if (!timestamp || !signature) return false;

    // Refuse anything older than five minutes, so a captured delivery can't be sent again later. The
    // timestamp is inside what is signed, so it can't be changed without breaking the signature.
    if (!/^\d+$/.test(timestamp) || Math.abs(Date.now() / 1000 - Number(timestamp)) > 300) return false;

    const expected = createHmac("sha256", secret)
        .update(Buffer.concat([Buffer.from(`${timestamp}.`), body]))
        .digest();
    const given = Buffer.from(signature, "hex");
    return given.length === expected.length && timingSafeEqual(given, expected);
}

createServer((req, res) => {
    // Sign the bytes exactly as they arrived: parsing and re-serialising the JSON first changes them.
    const chunks = [];
    req.on("data", (chunk) => chunks.push(chunk));
    req.on("end", () => {
        const body = Buffer.concat(chunks);

        if (!isGenuine(req.headers["x-training-timestamp"], body, req.headers["x-training-signature"])) {
            res.writeHead(401).end();
            return;
        }

        const delivery = req.headers["x-training-delivery-id"];

        if (!seen.has(delivery)) {
            seen.add(delivery);
            // Your work goes here. Answer quickly: a slow answer is retried as a failure.
            console.log(`${delivery}:`, body.length ? JSON.parse(body) : {});
        }

        res.writeHead(200).end();
    });
}).listen(port, () => console.log(`Listening on http://localhost:${port}/hooks/prediction`));

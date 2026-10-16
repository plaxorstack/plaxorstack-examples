// Orders the hotels of a new search so the ones a traveller is most likely to book come first.
// Needs Node 18 or later. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
const apiUrl = process.env.PLAXOR_API_URL ?? "https://api.plaxorstack.com";
const apiKey = process.env.PLAXOR_API_KEY;
const modelId = process.env.PLAXOR_MODEL_ID;

if (!apiKey || !modelId) {
    console.error("Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first.");
    process.exit(1);
}

// hotel, distance from the centre in km, guest rating out of 10
const candidates = [
    { hotel: "Harbour View", distance: 0.6, rating: 7.1 },
    { hotel: "Hilltop Residence", distance: 9.5, rating: 9.6 },
    { hotel: "Station Lodge", distance: 1.1, rating: 8.8 },
    { hotel: "Lakeside Retreat", distance: 4.0, rating: 9.2 },
    { hotel: "City Nest", distance: 0.3, rating: 6.4 },
];

for (const candidate of candidates) {
    const response = await fetch(`${apiUrl}/predict`, {
        method: "POST",
        headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
        body: JSON.stringify({
            trainingTypeId: Number(modelId),
            inputs: { distance_km: String(candidate.distance), guest_rating: String(candidate.rating) },
        }),
    });

    // A rejected key (401) has no body, so don't assume JSON.
    const body = await response.text();
    const result = body ? JSON.parse(body) : {};

    if (!response.ok || !result.success) {
        console.error(`${response.status}: ${result.message ?? response.statusText}`);
        process.exit(1);
    }

    candidate.score = result.score;
}

// A ranking score has no unit: it only says which item goes above which.
candidates
    .sort((a, b) => b.score - a.score)
    .forEach((c, i) => console.log(`${i + 1}. ${c.hotel.padEnd(18)} ${String(c.distance).padStart(4)} km  ${c.rating}/10   score ${c.score.toFixed(2)}`));

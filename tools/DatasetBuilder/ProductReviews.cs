namespace DatasetBuilder;

// Clothing-shop reviews, each about one to three things at once. A review that praises the fabric
// and complains about the courier is about quality AND delivery, which is why this is multi-label:
// a single-label model would have to pick one and lose the other.
internal static class ProductReviews
{
    private const int Count = 600;

    private static readonly Dictionary<string, string[]> Sentences = new()
    {
        ["delivery"] =
        [
            "It arrived a day earlier than promised.",
            "The parcel took almost two weeks to arrive.",
            "Delivery was quick and the courier was friendly.",
            "The box was left in the rain outside my door.",
            "Tracking said delivered but it only came three days later.",
            "Shipping was fast, ordered on Monday and wore it on Wednesday.",
            "The package was crushed when it got here.",
            "It came well packed and right on time.",
            "I had to collect it from the depot because the driver never rang.",
            "Express shipping was worth every cent.",
        ],
        ["quality"] =
        [
            "The fabric feels really soft and thick.",
            "The seams started coming apart after the first wash.",
            "Very well made, the stitching is perfect.",
            "The colour faded badly after two washes.",
            "It feels cheap and thin, almost see-through.",
            "The zip broke within a week.",
            "Great material, it still looks new after months.",
            "The print peeled off in the dryer.",
            "Solid buttons and a sturdy feel, it will last for years.",
            "Lots of loose threads everywhere.",
        ],
        ["price"] =
        [
            "Excellent value for the money.",
            "Way too expensive for what you get.",
            "I got it in the sale and it was a bargain.",
            "Cheaper than in the shops for the same thing.",
            "Overpriced, I've seen the same item for half the price.",
            "Worth paying a little extra for.",
            "Not worth forty euros.",
            "A fair price for this kind of jacket.",
            "Good deal with the discount code.",
            "Pricey, but you get what you pay for.",
        ],
        ["sizing"] =
        [
            "It runs small, order one size up.",
            "The fit is perfect, true to size.",
            "Far too big, I had to send it back for a smaller one.",
            "The sleeves are too short for me.",
            "Fits exactly like the size chart says.",
            "Tight across the shoulders.",
            "The length is just right, even for someone tall.",
            "The medium fits like a large.",
            "Waist is fine but the legs are much too long.",
            "I followed the size guide and it fits beautifully.",
        ],
        ["service"] =
        [
            "Customer service sorted out my problem in minutes.",
            "Nobody answered my emails for a week.",
            "The support team was very helpful when I asked about an exchange.",
            "I'm still waiting for my refund and nobody replies.",
            "The chat agent was polite and quick.",
            "Returning it was easy and the refund came fast.",
            "I was passed between three people on the phone.",
            "They sent a replacement without any fuss.",
            "The return label never arrived despite asking twice.",
            "Friendly staff who really went out of their way.",
        ],
    };

    private static readonly string[] Products =
    [
        "jacket", "jeans", "hoodie", "dress", "shirt", "sweater", "coat", "trainers", "t-shirt", "skirt",
    ];

    private static readonly string[] Openers =
    [
        "Bought this {product} for my partner.", "Ordered the {product} in blue.", "My second {product} from this shop.",
        "Got this {product} as a birthday present.", "", "", "", "", "",
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var random = new Random(20261017);
        var topics = Sentences.Keys.ToArray();
        var rows = new List<object[]>();

        for (var i = 0; i < Count; i++)
        {
            // Half the reviews mention two things and a fifth mention three: multi-label training is
            // refused when no row carries more than one label.
            var roll = random.NextDouble();
            var howMany = roll < 0.3 ? 1 : roll < 0.8 ? 2 : 3;
            var chosen = topics.OrderBy(_ => random.Next()).Take(howMany).ToArray();
            var product = Products[random.Next(Products.Length)];

            var parts = new List<string>();
            var opener = Openers[random.Next(Openers.Length)].Replace("{product}", product);
            if (opener.Length > 0) parts.Add(opener);
            parts.AddRange(chosen.Select(t => Sentences[t][random.Next(Sentences[t].Length)]));

            var labels = string.Join(';', topics.Where(chosen.Contains));
            rows.Add([string.Join(' ', parts), labels]);
        }

        var folder = RepoPaths.DataFolder(repoRoot, Path.Combine("text", "tag-product-reviews"));
        Csv.Write(Path.Combine(folder, "reviews.csv"), ["review", "topics"], rows);
        return Task.CompletedTask;
    }
}

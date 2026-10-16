namespace DatasetBuilder;

// An online shop's star ratings. Each customer has one main interest and one side interest, which is
// the structure a recommender has to discover: nobody tells it which products belong together.
internal static class ProductRatings
{
    private const int Customers = 250;

    private static readonly string[][] Catalog =
    [
        ["Trail running shoes", "Camping stove", "Two-person tent", "Hiking backpack 40L", "Headlamp", "Trekking poles", "Insulated water bottle", "Sleeping bag"],
        ["Chef's knife", "Cast iron skillet", "Coffee grinder", "Pour-over kettle", "Bamboo cutting board", "Stand mixer", "Spice rack", "Pasta maker"],
        ["Mechanical keyboard", "Gaming mouse", "Wireless headset", "Controller charging dock", "27-inch monitor", "XL mouse pad", "Streaming microphone", "Capture card"],
        ["E-reader", "Reading lamp", "Bookends", "Leather notebook", "Fountain pen", "Clip-on book light", "Book subscription box", "Bookshelf organiser"],
        ["Yoga mat", "Adjustable dumbbells", "Resistance bands", "Foam roller", "Jump rope", "Fitness tracker", "Kettlebell 12 kg", "Protein shaker"],
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var random = new Random(42);
        var rows = new List<object[]>();

        for (var c = 1; c <= Customers; c++)
        {
            var customer = $"C{c:0000}";
            var main = random.Next(Catalog.Length);
            var side = (main + 1 + random.Next(Catalog.Length - 1)) % Catalog.Length;
            var rated = new HashSet<string>();
            var count = random.Next(14, 23);

            while (rated.Count < count)
            {
                var roll = random.NextDouble();
                var group = roll < 0.55 ? main : roll < 0.80 ? side : random.Next(Catalog.Length);
                var product = Catalog[group][random.Next(Catalog[group].Length)];

                if (!rated.Add(product))
                    continue;

                var mean = group == main ? 4.4 : group == side ? 3.6 : 2.2;
                var stars = (int)Math.Clamp(Math.Round(Gaussian.Next(random, mean, 0.7)), 1, 5);
                rows.Add([customer, product, stars]);
            }
        }

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/recommend-products"), "product-ratings.csv"),
            ["customer_id", "product", "rating"], rows);

        Console.WriteLine($"  {rows.Count} rows");
        return Task.CompletedTask;
    }
}

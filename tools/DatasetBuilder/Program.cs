using DatasetBuilder;

// Rebuilds every sample's data/ folder from the original public sources, or from a fixed seed for
// the scenario datasets. Run from anywhere:
//
//   dotnet run --project tools/DatasetBuilder
//
// Downloads are cached in tools/DatasetBuilder/.cache, so a second run works offline.

var repoRoot = RepoPaths.FindRoot();
var sources = new SourceCache(Path.Combine(repoRoot, "tools", "DatasetBuilder", ".cache"));

var steps = new (string Name, Func<Task> Run)[]
{
    ("tabular/forecast-bike-rentals", () => BikeRentals.BuildAsync(sources, repoRoot)),
    ("tabular/predict-online-purchase", () => OnlinePurchase.BuildAsync(sources, repoRoot)),
    ("tabular/grade-beans-by-variety", () => BeanVarieties.BuildAsync(sources, repoRoot)),
    ("tabular/segment-wholesale-customers", () => WholesaleCustomers.BuildAsync(sources, repoRoot)),
    ("tabular/recommend-products", () => ProductRatings.BuildAsync(repoRoot)),
    ("tabular/rank-hotel-search-results", () => HotelSearch.BuildAsync(repoRoot)),
    ("tabular/spot-unusual-server-load", () => ServerMetrics.BuildAsync(repoRoot)),
    ("tabular/forecast-bakery-sales", () => BakerySales.BuildAsync(repoRoot)),
    ("text/filter-spam-messages", () => SpamMessages.BuildAsync(sources, repoRoot)),
    ("text/route-support-tickets", () => SupportTickets.BuildAsync(repoRoot)),
    ("text/tag-product-reviews", () => ProductReviews.BuildAsync(repoRoot)),
    ("text/answer-questions-from-faq", () => HelpCentreFaq.BuildAsync(repoRoot)),
    ("image/detect-leaf-disease", () => LeafDisease.BuildAsync(sources, repoRoot)),
    ("image/tag-fabric-swatches", () => FabricSwatches.BuildAsync(repoRoot)),
};

var only = args.FirstOrDefault();

foreach (var (name, run) in steps)
{
    if (only is not null && !name.Contains(only, StringComparison.OrdinalIgnoreCase))
        continue;

    Console.WriteLine($"→ {name}");
    await run();
}

Console.WriteLine("Done.");

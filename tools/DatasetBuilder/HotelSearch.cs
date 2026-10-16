namespace DatasetBuilder;

// A travel site's search log: for each search, the hotels shown and how useful each turned out
// (3 = booked, 2 = opened and saved, 1 = opened, 0 = ignored). Travellers trade distance against
// reviews, which is what the ranker has to learn.
internal static class HotelSearch
{
    private const int Searches = 500;
    private const int HotelsPerSearch = 8;

    private static readonly string[] Names =
    [
        "Hotel Aurora", "Riverside Inn", "Grand Central", "Old Town Suites", "Harbour View", "Park Lane Hotel",
        "The Linden", "Station Lodge", "Garden Court", "Skyline Tower", "Maple House", "Bridge Hotel",
        "City Nest", "Marina Bay Rooms", "The Courtyard", "Hilltop Residence", "Market Square Hotel", "Ivy Gate",
        "Northern Lights", "Cathedral View", "Lakeside Retreat", "Union Hotel", "Copper Kettle Inn", "Blue Door B&B",
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var random = new Random(7);
        var rows = new List<object[]>();

        for (var s = 1; s <= Searches; s++)
        {
            var search = $"S{s:0000}";

            var shown = Names.OrderBy(_ => random.Next()).Take(HotelsPerSearch).Select(name =>
            {
                // Most hotels on a results page are central; a few are far out.
                var distance = Math.Round(0.2 + Math.Pow(random.NextDouble(), 2) * 12, 1);
                var reviews = Math.Round(6.0 + random.NextDouble() * 3.8, 1);
                var appeal = 0.9 * reviews - 0.35 * distance + Gaussian.Next(random, 0, 0.5);
                return (Name: name, Distance: distance, Reviews: reviews, Appeal: appeal);
            }).OrderByDescending(h => h.Appeal).ToList();

            for (var position = 0; position < shown.Count; position++)
            {
                var relevance = position switch { 0 => 3, <= 2 => 2, <= 4 => 1, _ => 0 };
                var hotel = shown[position];
                rows.Add([search, hotel.Name, hotel.Distance, hotel.Reviews, relevance]);
            }
        }

        // Shuffled within each search so the file does not already list the answer in order.
        var output = rows.GroupBy(r => r[0]).SelectMany(g => g.OrderBy(_ => random.Next()));

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/rank-hotel-search-results"), "hotel-searches.csv"),
            ["search_id", "hotel", "distance_km", "guest_rating", "relevance"], output);

        Console.WriteLine($"  {rows.Count} rows");
        return Task.CompletedTask;
    }
}

namespace DatasetBuilder;

// UCI Wholesale Customers (Cardoso, 2014): annual spend of a Portuguese distributor's clients.
// CC BY 4.0.
internal static class WholesaleCustomers
{
    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        using var zip = await sources.OpenZipAsync(
            "https://archive.ics.uci.edu/static/public/292/wholesale+customers.zip", "wholesale-customers.zip");

        // Columns: Channel,Region,Fresh(2),Milk,Grocery(4),Frozen,Detergents_Paper,Delicassen
        // Channel (1 = hotel/restaurant/café, 2 = retail) is deliberately left out: it is the answer
        // the clustering should find on its own, and the README uses it to check the result.
        var rows = SourceCache.ReadLines(zip, "Wholesale customers data.csv")
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(Csv.Split)
            .Select(c => new object[] { int.Parse(c[2]), int.Parse(c[4]) })
            .ToList();

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/segment-wholesale-customers"), "customer-spend.csv"),
            ["fresh_food_spend", "grocery_spend"], rows);

        Console.WriteLine($"  {rows.Count} rows");
    }
}

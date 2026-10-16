namespace DatasetBuilder;

// UCI Online Shoppers Purchasing Intention (Sakar et al., 2018). CC BY 4.0.
internal static class OnlinePurchase
{
    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        using var zip = await sources.OpenZipAsync(
            "https://archive.ics.uci.edu/static/public/468/online+shoppers+purchasing+intention+dataset.zip", "online-shoppers.zip");

        // Columns: ...,PageValues(8),...,VisitorType(15),Weekend(16),Revenue(17)
        var rows = SourceCache.ReadLines(zip, "online_shoppers_intention.csv")
            .Skip(1)
            .Select(Csv.Split)
            .Select(c => new object[]
            {
                Math.Round(Csv.Number(c[8]), 2),
                c[15] switch { "Returning_Visitor" => "returning", "New_Visitor" => "new", _ => "other" },
                // The source column is TRUE/FALSE; lower-cased to match the other samples.
                c[17].Equals("TRUE", StringComparison.OrdinalIgnoreCase) ? "true" : "false"
            })
            .ToList();

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/predict-online-purchase"), "store-sessions.csv"),
            ["page_value", "visitor_type", "purchased"], rows);

        Console.WriteLine($"  {rows.Count} rows");
    }
}

namespace DatasetBuilder;

// UCI SMS Spam Collection (Almeida & Hidalgo, 2011). CC BY 4.0.
internal static class SpamMessages
{
    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        using var zip = await sources.OpenZipAsync(
            "https://archive.ics.uci.edu/static/public/228/sms+spam+collection.zip", "sms-spam.zip");

        // One message per line: "ham<TAB>text" or "spam<TAB>text".
        var rows = SourceCache.ReadLines(zip, "SMSSpamCollection")
            .Select(l => l.Split('\t', 2))
            .Where(p => p.Length == 2 && !string.IsNullOrWhiteSpace(p[1]))
            .Select(p => new object[] { p[1].Trim(), p[0] == "spam" ? "spam" : "normal" })
            .ToList();

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "text/filter-spam-messages"), "sms-messages.csv"),
            ["message", "category"], rows);

        Console.WriteLine($"  {rows.Count} rows");
    }
}

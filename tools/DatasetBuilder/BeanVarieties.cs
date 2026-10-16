using System.Globalization;

namespace DatasetBuilder;

// UCI Dry Bean Dataset (Koklu & Ozkan, 2020): measurements taken by a camera over a sorting line.
// CC BY 4.0.
internal static class BeanVarieties
{
    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        using var zip = await sources.OpenZipAsync(
            "https://archive.ics.uci.edu/static/public/602/dry+bean+dataset.zip", "dry-bean.zip");

        // ARFF: after @DATA, Area,Perimeter,MajorAxisLength(2),MinorAxisLength(3),...,Class(16)
        var rows = SourceCache.ReadLines(zip, "DryBeanDataset/Dry_Bean_Dataset.arff")
            .SkipWhile(l => !l.StartsWith("@DATA", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(Csv.Split)
            .Select(c => new object[]
            {
                Math.Round(Csv.Number(c[2]), 1),
                Math.Round(Csv.Number(c[3]), 1),
                CultureInfo.InvariantCulture.TextInfo.ToTitleCase(c[16].Trim().ToLowerInvariant())
            })
            .ToList();

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/grade-beans-by-variety"), "bean-measurements.csv"),
            ["length_px", "width_px", "variety"], rows);

        Console.WriteLine($"  {rows.Count} rows");
    }
}

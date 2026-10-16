namespace DatasetBuilder;

// UCI Bike Sharing (Capital Bikeshare, Washington D.C., 2011–2012), hourly. CC BY 4.0.
internal static class BikeRentals
{
    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        using var zip = await sources.OpenZipAsync(
            "https://archive.ics.uci.edu/static/public/275/bike+sharing+dataset.zip", "bike-sharing.zip");

        // Columns: instant,dteday,season,yr,mnth,hr,holiday,weekday,workingday,weathersit,temp,atemp,hum,windspeed,casual,registered,cnt
        // temp is normalised by the source as °C / 41.
        var rows = SourceCache.ReadLines(zip, "hour.csv")
            .Skip(1)
            .Select(Csv.Split)
            .Select(c => new object[] { int.Parse(c[5]), Math.Round(Csv.Number(c[10]) * 41, 1), int.Parse(c[16]) })
            .ToList();

        Csv.Write(Path.Combine(RepoPaths.DataFolder(repoRoot, "tabular/forecast-bike-rentals"), "bike-rentals.csv"),
            ["hour", "temperature_c", "rentals"], rows);

        Console.WriteLine($"  {rows.Count} rows");
    }
}

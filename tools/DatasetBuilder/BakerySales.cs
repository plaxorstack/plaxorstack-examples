namespace DatasetBuilder;

// Loaves sold a day at three bakeries of one small chain, January 2025 to the end of September 2026.
// Each shop has its own size and its own week: the station kiosk sells to commuters and is quiet at
// weekends, the two high-street shops peak on Saturday. Promotion days are planned weeks ahead, so a
// forecast may use them; on one a shop sells about a third more. Every day is present and none is
// blank, because a forecast model refuses gaps.
internal static class BakerySales
{
    private static readonly (string Name, double Base, double[] Week)[] Shops =
    [
        // Monday … Sunday, as a share of the shop's ordinary day.
        ("harbour-street", 180, [0.90, 0.92, 0.95, 1.00, 1.15, 1.45, 1.20]),
        ("market-square", 120, [0.85, 0.88, 0.90, 0.95, 1.10, 1.60, 1.05]),
        ("station-kiosk", 140, [1.15, 1.15, 1.12, 1.10, 1.05, 0.55, 0.40]),
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var random = new Random(20261028);
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2026, 9, 30);
        var rows = new List<object[]>();

        foreach (var (name, baseline, week) in Shops)
        {
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var promotion = IsPromotion(name, date);
                var weekday = ((int)date.DayOfWeek + 6) % 7;
                var years = (date - start).TotalDays / 365.25;

                // Slow growth, fewer loaves in the summer holidays, more in the weeks before Christmas.
                var season = 1 - 0.10 * Math.Exp(-Math.Pow((date.DayOfYear - 215) / 20.0, 2))
                               + 0.15 * Math.Exp(-Math.Pow((date.DayOfYear - 352) / 8.0, 2));
                var expected = baseline * week[weekday] * season * (1 + 0.06 * years) * (promotion ? 1.35 : 1);
                var sold = Math.Max(0, Math.Round(expected * Gaussian.Next(random, 1, 0.07)));

                rows.Add([date.ToString("yyyy-MM-dd"), name, sold, promotion ? 1 : 0]);
            }
        }

        var folder = RepoPaths.DataFolder(repoRoot, Path.Combine("tabular", "forecast-bakery-sales"));
        Csv.Write(Path.Combine(folder, "bakery-sales.csv"), ["date", "shop", "loaves_sold", "on_promotion"], rows);
        return Task.CompletedTask;
    }

    // Promotions follow a plan rather than chance, as real ones do: every shop on the first Tuesday
    // of the month, and each shop on its own day every third week. A random pattern would be one a
    // caller could not know ahead, which is the only kind of column a forecast may not use.
    private static bool IsPromotion(string shop, DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Tuesday && date.Day <= 7)
            return true;

        var week = (date - new DateTime(2024, 12, 30)).Days / 7;
        return shop switch
        {
            "harbour-street" => week % 3 == 0 && date.DayOfWeek == DayOfWeek.Thursday,
            "market-square" => week % 3 == 1 && date.DayOfWeek == DayOfWeek.Wednesday,
            _ => week % 3 == 2 && date.DayOfWeek == DayOfWeek.Monday,
        };
    }
}

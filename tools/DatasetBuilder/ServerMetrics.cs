namespace DatasetBuilder;

// A week of monitoring for a company's internal ordering system, a reading every 5 minutes. CPU
// follows traffic; a handful of incidents break that link. Anomaly detection learns the link, not a
// fixed CPU limit, so it must catch 85% CPU at 3 a.m. while ignoring 75% CPU at the afternoon peak.
internal static class ServerMetrics
{
    private const int Days = 7;
    private const int ReadingsPerDay = 24 * 12;

    // Moments when something went wrong, as (day, hour, minute, requests, cpu). Night-time CPU with no
    // traffic behind it is a stuck job or something that should not be running; peak traffic with an
    // idle CPU is requests failing before they do any work; 97% at 11:05 is a slow query.
    private static readonly (int Day, int Hour, int Minute, double Requests, double Cpu)[] Incidents =
    [
        (1, 3, 10, 240, 86),
        (1, 3, 15, 255, 88),
        (1, 3, 20, 231, 87),
        (3, 2, 40, 190, 64),
        (3, 2, 45, 205, 66),
        (4, 19, 30, 1960, 14),
        (4, 19, 35, 2010, 12),
        (4, 19, 40, 1985, 13),
        (5, 11, 5, 1450, 97),
        (6, 4, 0, 160, 71),
        (6, 4, 5, 172, 73),
    ];

    public static Task BuildAsync(string repoRoot)
    {
        var random = new Random(20261016);
        var start = new DateTime(2026, 9, 7);
        var incidents = Incidents.ToDictionary(i => (i.Day, i.Hour, i.Minute));
        var rows = new List<object[]>();

        for (var day = 0; day < Days; day++)
        {
            var weekend = day >= 5;

            for (var slot = 0; slot < ReadingsPerDay; slot++)
            {
                var time = start.AddDays(day).AddMinutes(slot * 5);
                double requests, cpu;

                if (incidents.TryGetValue((day, time.Hour, time.Minute), out var incident))
                {
                    requests = incident.Requests;
                    cpu = incident.Cpu;
                }
                else
                {
                    requests = Math.Max(20, Traffic(time.Hour + time.Minute / 60.0, weekend) * Gaussian.Next(random, 1, 0.06));
                    cpu = Math.Clamp(4 + 0.035 * requests + Gaussian.Next(random, 0, 1.6), 1, 100);
                }

                rows.Add([time.ToString("yyyy-MM-dd HH:mm"), Math.Round(requests), Math.Round(cpu, 1)]);
            }
        }

        var folder = RepoPaths.DataFolder(repoRoot, Path.Combine("tabular", "spot-unusual-server-load"));
        Csv.Write(Path.Combine(folder, "server-metrics.csv"), ["time", "requests_per_min", "cpu_percent"], rows);
        return Task.CompletedTask;
    }

    // Requests per minute by hour of day for a company's internal ordering system: quiet outside
    // office hours, busy from 8 to 18 on weekdays, close to quiet at weekends. The changeovers are
    // short, so few ordinary readings sit halfway between quiet and busy.
    private static double Traffic(double hour, bool weekend)
    {
        const double quiet = 220;
        var busy = weekend ? 300 : 1900;

        if (hour < 7.75) return quiet;
        if (hour < 8.25) return quiet + (busy - quiet) * (hour - 7.75) / 0.5;
        if (hour < 18) return busy * (1 + 0.08 * Math.Sin((hour - 8.25) / 9.75 * Math.PI));
        if (hour < 18.5) return busy - (busy - quiet) * (hour - 18) / 0.5;
        return quiet;
    }
}

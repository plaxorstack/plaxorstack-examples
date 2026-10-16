namespace DatasetBuilder;

internal static class Gaussian
{
    // Box–Muller: Random has no normal distribution of its own.
    public static double Next(Random random, double mean, double deviation)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = random.NextDouble();
        return mean + deviation * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
}

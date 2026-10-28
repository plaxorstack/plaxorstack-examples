using System.IO.Compression;
using SkiaSharp;

namespace DatasetBuilder;

// Close-up photos of fabric swatches for a shop's catalogue, each tagged with every colour in it and
// its pattern: "red;white;striped", "navy;dotted", "green" for a plain one. A folder per category
// cannot say that, which is the point of the multi-label task.
//
// Drawn, not photographed: no openly licensed photo set is tagged this way. Each swatch is turned at
// a random angle, lit unevenly and given a woven grain, so the model has to recognise a colour and a
// pattern rather than memorise a position.
internal static class FabricSwatches
{
    private const int Size = 224;
    private const int TrainCount = 480;

    private static readonly (string Name, SKColor Color)[] Colours =
    [
        ("red", new SKColor(196, 32, 38)),
        ("navy", new SKColor(28, 42, 92)),
        ("green", new SKColor(46, 130, 64)),
        ("yellow", new SKColor(238, 196, 40)),
        ("white", new SKColor(240, 238, 230)),
        ("black", new SKColor(30, 30, 32)),
    ];

    private static readonly string[] Patterns = ["striped", "dotted", "checked", "plain"];

    // Swatches the model never sees, one of each kind, written to data/try/ under the tags they carry.
    private static readonly (string Pattern, string[] Colours)[] HeldOut =
    [
        ("striped", ["red", "white"]),
        ("dotted", ["navy", "white"]),
        ("checked", ["green", "yellow"]),
        ("plain", ["black"]),
        ("striped", ["navy", "yellow", "white"]),
        ("dotted", ["black", "red"]),
    ];

    public static async Task BuildAsync(string repoRoot)
    {
        var data = RepoPaths.DataFolder(repoRoot, Path.Combine("image", "tag-fabric-swatches"));
        var tryFolder = Path.Combine(data, "try");
        Directory.CreateDirectory(tryFolder);

        var random = new Random(20261028);

        foreach (var (pattern, colours) in HeldOut)
        {
            var name = $"{string.Join('-', colours)}-{pattern}.jpg";
            await File.WriteAllBytesAsync(Path.Combine(tryFolder, name), Draw(random, pattern, colours));
        }

        var zipPath = Path.Combine(data, "fabric-swatches.zip");
        File.Delete(zipPath);

        using var output = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var table = new List<string> { "image,labels" };

        for (var i = 1; i <= TrainCount; i++)
        {
            var pattern = Patterns[random.Next(Patterns.Length)];
            var colours = PickColours(random, pattern);
            var path = $"swatches/swatch-{i:000}.jpg";

            // Stored, not deflated: a JPEG does not compress further.
            var entry = output.CreateEntry(path, CompressionLevel.NoCompression);
            await using (var target = entry.Open())
                await target.WriteAsync(Draw(random, pattern, colours));

            var labels = pattern == "plain" ? colours : [.. colours, pattern];
            table.Add($"{path},{string.Join(';', labels)}");
        }

        var labelsEntry = output.CreateEntry("labels.csv", CompressionLevel.Optimal);
        await using (var writer = new StreamWriter(labelsEntry.Open()))
        {
            writer.NewLine = "\n";
            foreach (var line in table)
                await writer.WriteLineAsync(line);
        }

        Console.WriteLine($"  {TrainCount} swatches to train, {HeldOut.Length} to try");
    }

    // A plain swatch is one colour; a patterned one two, and a stripe sometimes three.
    private static string[] PickColours(Random random, string pattern)
    {
        var count = pattern switch
        {
            "plain" => 1,
            "striped" when random.NextDouble() < 0.3 => 3,
            _ => 2,
        };

        return Colours.Select(c => c.Name).OrderBy(_ => random.Next()).Take(count).ToArray();
    }

    private static byte[] Draw(Random random, string pattern, string[] colourNames)
    {
        var colours = colourNames.Select(n => Shade(random, Colours.First(c => c.Name == n).Color)).ToArray();

        using var surface = SKSurface.Create(new SKImageInfo(Size, Size));
        var canvas = surface.Canvas;
        canvas.Clear(colours[0]);

        canvas.Save();
        canvas.RotateDegrees(random.Next(-35, 36), Size / 2f, Size / 2f);

        // Drawn well past the edges, so a turned pattern still covers the corners.
        const float from = -Size, to = Size * 2;

        using (var paint = new SKPaint { IsAntialias = true })
        {
            switch (pattern)
            {
                case "striped":
                    var width = random.Next(10, 26);
                    var stripe = 0;
                    for (var x = from; x < to; x += width * 2)
                    {
                        paint.Color = colours[1 + stripe++ % (colours.Length - 1)];
                        canvas.DrawRect(x, from, width, to - from, paint);
                    }
                    break;

                case "dotted":
                    var spacing = random.Next(22, 40);
                    var radius = spacing * (float)(0.18 + random.NextDouble() * 0.12);
                    paint.Color = colours[1];
                    for (var y = from; y < to; y += spacing)
                    for (var x = from + (y / spacing % 2 == 0 ? 0 : spacing / 2f); x < to; x += spacing)
                        canvas.DrawCircle(x, y, radius, paint);
                    break;

                case "checked":
                    var band = random.Next(14, 30);
                    paint.Color = colours[1].WithAlpha(170);
                    for (var p = from; p < to; p += band * 2)
                    {
                        canvas.DrawRect(p, from, band, to - from, paint);
                        canvas.DrawRect(from, p, to - from, band, paint);
                    }
                    break;
            }
        }

        canvas.Restore();

        // A woven grain and light from one side: what makes it read as cloth under a lamp.
        using (var grain = new SKPaint
               {
                   Shader = SKShader.CreatePerlinNoiseTurbulence(0.9f, 0.9f, 2, random.Next(1000)),
                   BlendMode = SKBlendMode.Overlay,
                   Color = SKColors.White.WithAlpha(90),
               })
            canvas.DrawRect(0, 0, Size, Size, grain);

        var lightX = random.Next(0, Size);
        var lightY = random.Next(0, Size);
        using (var light = new SKPaint
               {
                   Shader = SKShader.CreateRadialGradient(new SKPoint(lightX, lightY), Size * 1.1f,
                       [SKColors.White.WithAlpha(50), SKColors.Black.WithAlpha(70)], SKShaderTileMode.Clamp),
               })
            canvas.DrawRect(0, 0, Size, Size, light);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }

    // The same dye never comes out quite the same twice, and a camera adds its own cast.
    private static SKColor Shade(Random random, SKColor colour)
    {
        var factor = 0.85 + random.NextDouble() * 0.25;
        byte Channel(byte c) => (byte)Math.Clamp(c * factor + random.Next(-10, 11), 0, 255);
        return new SKColor(Channel(colour.Red), Channel(colour.Green), Channel(colour.Blue));
    }
}

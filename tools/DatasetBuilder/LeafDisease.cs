using System.IO.Compression;
using SkiaSharp;

namespace DatasetBuilder;

// Beans leaf images (Makerere AI Lab, Uganda), photographed in the field with phones. MIT licence.
// The validation and test splits together: ~260 photos, enough to train inside a trial's GPU minutes.
internal static class LeafDisease
{
    private const int HeldOutPerCategory = 3;
    private const int LongestSide = 320;

    public static async Task BuildAsync(SourceCache sources, string repoRoot)
    {
        var data = RepoPaths.DataFolder(repoRoot, "image/detect-leaf-disease");
        var tryFolder = Path.Combine(data, "try");
        Directory.CreateDirectory(tryFolder);

        var images = new List<(string Category, string Name, byte[] Bytes)>();

        foreach (var split in new[] { "validation", "test" })
        {
            using var zip = await sources.OpenZipAsync(
                $"https://huggingface.co/datasets/AI-Lab-Makerere/beans/resolve/main/data/{split}.zip", $"beans-{split}.zip");

            foreach (var entry in zip.Entries.Where(e => e.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            {
                var segments = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
                await using var stream = entry.Open();
                images.Add((segments[^2], entry.Name, Shrink(stream)));
            }
        }

        var zipPath = Path.Combine(data, "leaf-photos.zip");
        File.Delete(zipPath);

        using var output = ZipFile.Open(zipPath, ZipArchiveMode.Create);

        foreach (var category in images.GroupBy(i => i.Category).OrderBy(g => g.Key))
        {
            var ordered = category.OrderBy(i => i.Name, StringComparer.Ordinal).ToList();

            // The first few of each category stay out of the training ZIP, so the reader has photos
            // the model has never seen to try it on.
            var n = 0;

            foreach (var image in ordered.Take(HeldOutPerCategory))
                await File.WriteAllBytesAsync(Path.Combine(tryFolder, $"{category.Key}-{++n}.jpg"), image.Bytes);

            foreach (var image in ordered.Skip(HeldOutPerCategory))
            {
                // Stored, not deflated: a JPEG does not compress further.
                var entry = output.CreateEntry($"{category.Key}/{image.Name}", CompressionLevel.NoCompression);
                await using var target = entry.Open();
                await target.WriteAsync(image.Bytes);
            }

            Console.WriteLine($"  {category.Key}: {ordered.Count - HeldOutPerCategory} to train, {HeldOutPerCategory} to try");
        }
    }

    private static byte[] Shrink(Stream source)
    {
        using var original = SKBitmap.Decode(source);
        var scale = Math.Min(1.0, (double)LongestSide / Math.Max(original.Width, original.Height));
        var size = new SKImageInfo((int)Math.Round(original.Width * scale), (int)Math.Round(original.Height * scale));

        using var resized = original.Resize(size, new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var data = resized.Encode(SKEncodedImageFormat.Jpeg, 85);
        return data.ToArray();
    }
}

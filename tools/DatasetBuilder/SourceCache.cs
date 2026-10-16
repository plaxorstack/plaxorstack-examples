using System.IO.Compression;

namespace DatasetBuilder;

internal sealed class SourceCache(string folder)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    public async Task<ZipArchive> OpenZipAsync(string url, string fileName)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);

        if (!File.Exists(path))
        {
            Console.WriteLine($"  downloading {url}");
            var partial = path + ".part";

            await using (var source = await Http.GetStreamAsync(url))
            await using (var target = File.Create(partial))
                await source.CopyToAsync(target);

            File.Move(partial, path);
        }

        return ZipFile.OpenRead(path);
    }

    public static IEnumerable<string> ReadLines(ZipArchive zip, string entryName)
    {
        var entry = zip.GetEntry(entryName)
            ?? throw new InvalidOperationException($"{entryName} is missing from the download; the source may have changed.");

        using var reader = new StreamReader(entry.Open());

        while (reader.ReadLine() is { } line)
            yield return line;
    }
}

using System.Globalization;
using System.Text;

namespace DatasetBuilder;

internal static class Csv
{
    // Comma-separated, invariant decimal point, UTF-8 without BOM: what the platform's importer and
    // every spreadsheet read the same way.
    public static void Write(string path, string[] header, IEnumerable<object[]> rows)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.NewLine = "\n";
        writer.WriteLine(string.Join(',', header.Select(Quote)));

        foreach (var row in rows)
            writer.WriteLine(string.Join(',', row.Select(Format)));
    }

    // Splits one line of a plain comma-separated source with no quoted commas.
    public static string[] Split(string line) => line.Split(',');

    public static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static string Format(object value) => value switch
    {
        double d => d.ToString("0.####", CultureInfo.InvariantCulture),
        float f => f.ToString("0.####", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => Quote(value.ToString() ?? "")
    };

    private static string Quote(string text) =>
        text.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
}

#:property PublishAot=false
// Sends a night's tickets to your model in one batch and saves the results file its action writes.
// Needs .NET 10. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
//
//   dotnet run batch-results.cs                          # ../data/overnight-tickets.txt, one ticket per line
//   dotnet run batch-results.cs -- path/to/tickets.txt
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = long.Parse(Required("PLAXOR_MODEL_ID"));

var path = args.Length > 0 ? args[0] : Path.Combine("..", "data", "overnight-tickets.txt");
var tickets = File.ReadLines(path)
    .Select(line => line.Trim())
    .Where(line => line.Length > 0)
    .Take(32) // a batch holds at most 32 texts
    .ToList();

using var http = new HttpClient();

using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiUrl}/predict/text/batch")
{
    Content = JsonContent.Create(new { trainingTypeId = modelId, texts = tickets })
};
request.Headers.Add("X-Api-Key", apiKey);

using var response = await http.SendAsync(request);

// A rejected key (401) has no body, so don't assume JSON.
var body = await response.Content.ReadAsStringAsync();
using var document = JsonDocument.Parse(body.Length > 0 ? body : "{}");

if (!response.IsSuccessStatusCode)
{
    var message = document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("message", out var m)
        ? m.ToString()
        : response.ReasonPhrase;
    Console.Error.WriteLine($"{(int)response.StatusCode}: {message}");
    return 1;
}

var i = 0;
foreach (var result in document.RootElement.EnumerateArray())
{
    var label = result.GetProperty("success").GetBoolean()
        ? result.GetProperty("predictedLabel").GetString()
        : $"failed: {result.GetProperty("message")}";
    var ticket = tickets[i++];
    Console.WriteLine($"{label,-10} {ticket[..Math.Min(70, ticket.Length)]}");
}

// One header per Results file action that matched at least one row. None: no such action on this
// model, it is set to Ask first, or no row met its condition.
var links = response.Headers.TryGetValues("X-Result-File", out var values) ? values.ToList() : [];

if (links.Count == 0)
{
    Console.WriteLine("\nNo results file: no Run immediately action matched any row.");
    return 0;
}

for (var n = 1; n <= links.Count; n++)
{
    // The worker writes the file a few seconds after the batch answers. Until then: 202 + Retry-After.
    // The link is its own authorization, so this client has no API key on it.
    using var download = new HttpClient();
    HttpResponseMessage file;
    var attempts = 0;

    while (true)
    {
        file = await download.GetAsync(links[n - 1]);
        if (file.StatusCode != HttpStatusCode.Accepted || ++attempts >= 24) break;
        await Task.Delay(file.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5));
        file.Dispose();
    }

    using (file)
    {
        if (file.StatusCode != HttpStatusCode.OK)
        {
            Console.Error.WriteLine($"Results file {n}: {(int)file.StatusCode} {await file.Content.ReadAsStringAsync()}");
            return 1;
        }

        var bytes = await file.Content.ReadAsByteArrayAsync();
        var isZip = file.Content.Headers.ContentType?.MediaType?.Contains("zip") == true;
        var name = $"results-{n}.{(isZip ? "zip" : "pdf")}";
        await File.WriteAllBytesAsync(name, bytes);
        Console.WriteLine($"\nSaved {name} ({bytes.Length:N0} bytes)");
    }
}

return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

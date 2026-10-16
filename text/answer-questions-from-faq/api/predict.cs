#:property PublishAot=false
// Searches your help articles for the ones that answer a customer's question.
// Needs .NET 10: dotnet run predict.cs -- ["your question"]   (default: "where is my parcel")
// Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");
var question = args.Length > 0 ? args[0] : "where is my parcel";

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

var response = await http.PostAsJsonAsync($"{apiUrl}/predict/text", new
{
    trainingTypeId = long.Parse(modelId),
    text = question,
    top = 3,
    minScore = 0.3,
});

// A rejected key (401) has no body, so don't assume JSON.
var body = await response.Content.ReadAsStringAsync();
using var document = JsonDocument.Parse(body.Length > 0 ? body : "{}");
var result = document.RootElement;

if (!response.IsSuccessStatusCode || !result.TryGetProperty("success", out var success) || !success.GetBoolean())
{
    var message = result.TryGetProperty("message", out var m) ? m.ToString() : response.ReasonPhrase;
    Console.Error.WriteLine($"{(int)response.StatusCode}: {message}");
    return 1;
}

Console.WriteLine($"> {question}");
var matches = result.GetProperty("matches");

// Empty, not an error, when nothing is close enough: hand over to a person.
if (matches.GetArrayLength() == 0)
    Console.WriteLine("  No article answers that. Passing you to our team.");

foreach (var match in matches.EnumerateArray())
{
    var payload = match.TryGetProperty("payload", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
    var title = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("question", out var q)
        ? q.GetString()
        : match.GetProperty("id").GetString();
    var link = payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("link", out var l) ? l.GetString() : "";

    Console.WriteLine($"  {match.GetProperty("score").GetDouble():0.00}  {title}");
    Console.WriteLine($"        {link}");
}

return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

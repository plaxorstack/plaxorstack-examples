#:property PublishAot=false
// Asks your model whether a server reading is unusual.
// Needs .NET 10: dotnet run predict.cs -- [requests_per_min] [cpu_percent]   (default: 250, 86)
// Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");
var requestsPerMin = args.Length > 0 ? args[0] : "250";
var cpuPercent = args.Length > 1 ? args[1] : "86";

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

var response = await http.PostAsJsonAsync($"{apiUrl}/predict", new
{
    trainingTypeId = long.Parse(modelId),
    inputs = new Dictionary<string, string> { ["requests_per_min"] = requestsPerMin, ["cpu_percent"] = cpuPercent },
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

var unusual = result.GetProperty("predictedLabel").GetString() == "True" ? "yes" : "no";
var score = result.GetProperty("score").GetDouble();
Console.WriteLine($"{requestsPerMin} requests/min at {cpuPercent}% CPU — unusual: {unusual} (score {score:0.00})");
return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

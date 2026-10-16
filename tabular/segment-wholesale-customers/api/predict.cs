#:property PublishAot=false
// Asks your model which customer segment a buyer belongs to.
// Needs .NET 10: dotnet run predict.cs
// Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

var response = await http.PostAsJsonAsync($"{apiUrl}/predict", new
{
    trainingTypeId = long.Parse(modelId),
    inputs = new Dictionary<string, string>
    {
        ["fresh_food_spend"] = "5000",
        ["grocery_spend"] = "30000",
    },
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

Console.WriteLine($"Segment: cluster {result.GetProperty("predictedLabel").GetString()}");
return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

#:property PublishAot=false
// Asks your model for a shop's next 14 days of sales.
// Needs .NET 10: dotnet run predict.cs -- [shop] [promotion days]   e.g. -- station-kiosk 5,12
// Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
using System.Net.Http.Json;
using System.Text.Json;

const int horizon = 14;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");
var shop = args.Length > 0 ? args[0] : "harbour-street";
var promotions = args.Length > 1
    ? args[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet()
    : [];

// One [on_promotion] per day ahead: the model was trained with it, so it needs the plan.
var covariates = Enumerable.Range(1, horizon).Select(day => new[] { promotions.Contains(day) ? 1f : 0f }).ToArray();

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

var response = await http.PostAsJsonAsync($"{apiUrl}/predict/forecast", new
{
    trainingTypeId = long.Parse(modelId),
    horizon,
    series = shop,
    covariates,
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

var forecast = result.GetProperty("forecast");
var points = forecast.GetProperty("points");
Console.WriteLine($"{forecast.GetProperty("series").GetString()}, {forecast.GetProperty("frequency").GetString()} — next {points.GetArrayLength()} days");

foreach (var point in points.EnumerateArray())
{
    var day = point.GetProperty("date").GetDateTime();
    Console.WriteLine($"{day:yyyy-MM-dd}  {day:ddd}  {point.GetProperty("value").GetDouble(),5:0} loaves   "
        + $"likely {point.GetProperty("lower").GetDouble():0}–{point.GetProperty("upper").GetDouble():0}");
}

return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

#:property PublishAot=false
// Orders the hotels of a new search so the ones a traveller is most likely to book come first.
// Needs .NET 10: dotnet run predict.cs
// Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");

// hotel, distance from the centre in km, guest rating out of 10
(string Hotel, double Distance, double Rating)[] candidates =
[
    ("Harbour View", 0.6, 7.1),
    ("Hilltop Residence", 9.5, 9.6),
    ("Station Lodge", 1.1, 8.8),
    ("Lakeside Retreat", 4.0, 9.2),
    ("City Nest", 0.3, 6.4),
];

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

var ranked = new List<(double Score, string Hotel, double Distance, double Rating)>();

foreach (var (hotel, distance, rating) in candidates)
{
    var response = await http.PostAsJsonAsync($"{apiUrl}/predict", new
    {
        trainingTypeId = long.Parse(modelId),
        inputs = new Dictionary<string, string>
        {
            ["distance_km"] = distance.ToString(CultureInfo.InvariantCulture),
            ["guest_rating"] = rating.ToString(CultureInfo.InvariantCulture),
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

    ranked.Add((result.GetProperty("score").GetDouble(), hotel, distance, rating));
}

// A ranking score has no unit: it only says which item goes above which.
var position = 0;

foreach (var (score, hotel, distance, rating) in ranked.OrderByDescending(r => r.Score))
    Console.WriteLine($"{++position}. {hotel,-18} {distance,4} km  {rating}/10   score {score:F2}");

return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

#:property PublishAot=false
// Sends a photo of a bean leaf to your model and prints what it sees.
// Needs .NET 10. Set PLAXOR_API_KEY and PLAXOR_MODEL_ID first — see the README one folder up.
//
//   dotnet run predict.cs                              # a held-out photo from ../data/try
//   dotnet run predict.cs -- path/to/your-leaf.jpg     # your own photo
using System.Net.Http.Headers;
using System.Text.Json;

var apiUrl = Environment.GetEnvironmentVariable("PLAXOR_API_URL") ?? "https://api.plaxorstack.com";
var apiKey = Required("PLAXOR_API_KEY");
var modelId = Required("PLAXOR_MODEL_ID");

var photo = args.Length > 0 ? args[0] : Path.Combine("..", "data", "try", "bean_rust-1.jpg");

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

using var image = new ByteArrayContent(await File.ReadAllBytesAsync(photo));
image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

using var form = new MultipartFormDataContent
{
    { new StringContent(modelId), "trainingTypeId" },
    { image, "image", Path.GetFileName(photo) },
};

var response = await http.PostAsync($"{apiUrl}/predict/image", form);

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

Console.WriteLine($"{Path.GetFileName(photo)}: {result.GetProperty("predictedLabel").GetString()} (score {result.GetProperty("score").GetDouble():F2})");
return 0;

static string Required(string name) =>
    Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Set {name} first.");

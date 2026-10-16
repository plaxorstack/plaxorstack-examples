#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false
// A small back end that keeps your PlaxorStack API key on the server.
// The browser calls POST /api/route-ticket with { text }; this server adds the key and the model id
// and forwards the request. Run: dotnet run Program.cs, then open the address it prints.
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

var apiUrl = builder.Configuration["PLAXOR_API_URL"] ?? "https://api.plaxorstack.com";
var apiKey = builder.Configuration["PLAXOR_API_KEY"] ?? throw new InvalidOperationException("Set PLAXOR_API_KEY first.");
var modelId = long.Parse(builder.Configuration["PLAXOR_MODEL_ID"] ?? throw new InvalidOperationException("Set PLAXOR_MODEL_ID first."));

builder.Services.AddHttpClient("plaxor", http =>
{
    http.BaseAddress = new Uri(apiUrl);
    http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    http.Timeout = TimeSpan.FromSeconds(15);
});

// Every prediction counts against YOUR monthly allowance, so a public endpoint needs a limit of its own.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-visitor", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();
app.UseRateLimiter();

// The page is shared with the Express example, one folder up. Run from this folder.
var publicFolder = new PhysicalFileProvider(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "public")));
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = publicFolder });
app.UseStaticFiles(new StaticFileOptions { FileProvider = publicFolder });

app.MapPost("/api/route-ticket", async (TicketRequest request, IHttpClientFactory factory, ILogger<TicketRequest> logger) =>
{
    // Accept only what this page needs. Never forward a model id or arbitrary JSON from the browser:
    // anyone can call this endpoint, not only your page.
    var text = request.Text?.Trim() ?? "";
    if (text.Length is 0 or > 4000)
        return Results.BadRequest(new { error = "Write between 1 and 4,000 characters." });

    try
    {
        var response = await factory.CreateClient("plaxor").PostAsJsonAsync("/predict/text", new { trainingTypeId = modelId, text });
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body.Length > 0 ? body : "{}");
        var result = document.RootElement;

        if (!response.IsSuccessStatusCode || !result.TryGetProperty("success", out var success) || !success.GetBoolean())
        {
            // Log the details for yourself; tell the visitor only that it failed.
            logger.LogError("PlaxorStack {Status}: {Message}", (int)response.StatusCode,
                result.TryGetProperty("message", out var message) ? message.ToString() : response.ReasonPhrase);
            return Results.Json(new { error = "The prediction service is unavailable." }, statusCode: 502);
        }

        // Return only what the page shows, not the whole response.
        return Results.Ok(new { team = result.GetProperty("predictedLabel").GetString() });
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
        logger.LogError(ex, "PlaxorStack request failed");
        return Results.Json(new { error = "The prediction service is unavailable." }, statusCode: 502);
    }
}).RequireRateLimiting("per-visitor");

app.Run();

internal sealed class TicketRequest
{
    public string? Text { get; set; }
}

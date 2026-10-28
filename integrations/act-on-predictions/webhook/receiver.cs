#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false
// Receives a Webhook action's deliveries and checks each one really came from PlaxorStack.
// Needs .NET 10. Set PLAXOR_WEBHOOK_SECRET to the card's signing secret first.
//
//   dotnet run receiver.cs       # listens on http://localhost:8080/hooks/prediction
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

var secret = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("PLAXOR_WEBHOOK_SECRET")
    ?? throw new InvalidOperationException("Set PLAXOR_WEBHOOK_SECRET first."));
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// Every retry of one delivery carries the same id. Keep these in your database in production: this
// set is forgotten when the process restarts.
var seen = new ConcurrentDictionary<string, bool>();

var app = WebApplication.Create(args);

app.MapMethods("/hooks/prediction", ["POST", "PUT", "PATCH"], async (HttpRequest request) =>
{
    // Sign the bytes exactly as they arrived: parsing and re-serialising the JSON first changes them.
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer);
    var body = buffer.ToArray();

    if (!IsGenuine(request.Headers["X-Training-Timestamp"], body, request.Headers["X-Training-Signature"]))
        return Results.Unauthorized();

    var delivery = request.Headers["X-Training-Delivery-Id"].ToString();

    if (seen.TryAdd(delivery, true))
    {
        // Your work goes here. Answer quickly: a slow answer is retried as a failure.
        Console.WriteLine($"{delivery}: {Encoding.UTF8.GetString(body)}");
    }

    return Results.Ok();
});

Console.WriteLine($"Listening on http://localhost:{port}/hooks/prediction");
app.Run($"http://localhost:{port}");

bool IsGenuine(string? timestamp, byte[] body, string? signature)
{
    // Refuse anything older than five minutes, so a captured delivery can't be sent again later. The
    // timestamp is inside what is signed, so it can't be changed without breaking the signature.
    if (!long.TryParse(timestamp, out var seconds) || Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - seconds) > 300)
        return false;

    byte[] given;
    try
    {
        given = Convert.FromHexString(signature ?? "");
    }
    catch (FormatException)
    {
        return false;
    }

    byte[] signed = [.. Encoding.UTF8.GetBytes($"{timestamp}."), .. body];
    var expected = HMACSHA256.HashData(secret, signed);
    return CryptographicOperations.FixedTimeEquals(given, expected);
}

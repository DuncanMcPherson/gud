using System.Security.Cryptography;
using System.Text;

namespace gud.Server;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-API-Key";
    
    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var configuredKey = Environment.GetEnvironmentVariable("GUD_API_KEY");

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedApiKey) || 
            configuredKey is null ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedApiKey.ToString()),
                Encoding.UTF8.GetBytes(configuredKey)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing API key");
            return;
        }

        await _next(context);
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpWeatherServer.Infrastructure;

public sealed class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var props = new Dictionary<string, object?>
        {
            ["http_method"] = context.Request.Method,
            ["http_path"] = context.Request.Path.ToString()
        };

        using var _ = logger.BeginTimedScope("http_request", props);

        await next(context);

        // Optional: status code log line (still in scope)
        logger.LogInformation("HTTP completed with status {status_code}", context.Response.StatusCode);
    }
}

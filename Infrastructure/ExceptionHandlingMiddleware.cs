using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpWeatherServer.Infrastructure;

/// <summary>
/// Catches unhandled exceptions, logs them, and returns:
/// - JSON-RPC style error envelope for /mcp
/// - ProblemDetails for other endpoints
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Never leak internals to callers
            logger.LogError(ex, "Unhandled exception while processing request.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // If response already started, can't write body
            if (context.Response.HasStarted)
                return;

            if (context.Request.Path.StartsWithSegments("/mcp"))
            {
                // Best-effort JSON-RPC error envelope (Internal error)
                // We don't know the original request id at this layer, so id=null.
                context.Response.ContentType = "application/json";

                var payload = new
                {
                    jsonrpc = "2.0",
                    id = (object?)null,
                    error = new
                    {
                        code = -32603,
                        message = "Internal error"
                    }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                return;
            }

            // Standard API error for non-MCP routes
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}

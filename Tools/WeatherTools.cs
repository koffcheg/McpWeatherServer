using McpWeatherServer.Domain;
using McpWeatherServer.Infrastructure;
using McpWeatherServer.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpWeatherServer.Tools;

[McpServerToolType]
public static class WeatherTools
{
    [McpServerTool, Description("Get today's weather for coordinates + timezone (IANA). Returns JSON.")]
    public static async Task<CallToolResult> GetTodayWeather(
        IOpenMeteoApiClient api,
        ILogger<WeatherToolsLogCategory> logger,
        [Description("Latitude, -90..90")] double latitude,
        [Description("Longitude, -180..180")] double longitude,
        [Description("IANA timezone, e.g. Europe/Kyiv")] string timezone = "Europe/Kyiv",
        CancellationToken ct = default)
    {
        var scopeProps = new Dictionary<string, object?>
        {
            ["tool"] = "get_today_weather",
            ["latitude"] = latitude,
            ["longitude"] = longitude,
            ["timezone"] = timezone
        };

        using var _ = logger.BeginTimedScope("mcp_tool", scopeProps);

        var reqR = WeatherRequest.Create(latitude, longitude, timezone);
        if (!reqR.IsSuccess)
            return McpResponses.ToolError(reqR.Error!);

        var req = reqR.Value!;
        var weatherR = await api.GetTodayAsync(req.Latitude, req.Longitude, req.Timezone, ct);

        return weatherR.Match(
            ok: McpResponses.Ok,
            fail: err =>
            {
                object? details = err is Error.Upstream u
                    ? new { u.StatusCode, u.RetryAfterSeconds }
                    : null;

                return McpResponses.ToolError(err, details);
            });
    }
}

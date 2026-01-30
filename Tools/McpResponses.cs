using McpWeatherServer.Domain;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace McpWeatherServer.Tools;

public static class McpResponses
{
    public static CallToolResult Ok<T>(T data)
        => new()
        {
            IsError = false,
            Content =
            [
                new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(data) }
            ]
        };

    public static CallToolResult ToolError(Error err, object? details = null)
    {
        var payload = details is null
            ? new { code = err.Code, message = err.Message }
            : new { code = err.Code, message = err.Message, details };

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock { Type = "text", Text = JsonSerializer.Serialize(payload) }
            ]
        };
    }
}

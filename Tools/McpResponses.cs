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
                new TextContentBlock
                {
                    // Type is read-only in this SDK; it's implicitly "text"
                    Text = JsonSerializer.Serialize(data)
                }
            ]
        };

    public static CallToolResult ToolError(Error err, object? details = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["code"] = err.Code,
            ["message"] = err.Message
        };

        if (details is not null)
            payload["details"] = details;

        return new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock
                {
                    Text = JsonSerializer.Serialize(payload)
                }
            ]
        };
    }
}

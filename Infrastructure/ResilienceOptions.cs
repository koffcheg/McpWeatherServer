using System.ComponentModel.DataAnnotations;

namespace McpWeatherServer.Infrastructure;

public sealed class CircuitBreakerPolicyOptions
{
    public const string SectionName = nameof(CircuitBreakerPolicyOptions);

    [Range(1, 3600)]
    public int BreakDurationSeconds { get; init; } = 30;
}

public sealed class RetryPolicyOptions
{
    public const string SectionName = nameof(RetryPolicyOptions);

    [Range(0, 20)]
    public int RetryCount { get; init; } = 3;
}

public sealed class TotalRequestTimeoutPolicyOptions
{
    public const string SectionName = nameof(TotalRequestTimeoutPolicyOptions);

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}

namespace McpWeatherServer.Domain;

public readonly record struct Result<T>(T? Value, Error? Error)
{
    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(Error error) => new(default, error);

    public TResult Match<TResult>(Func<T, TResult> ok, Func<Error, TResult> fail)
        => IsSuccess ? ok(Value!) : fail(Error!);
}

public abstract record Error(string Code, string Message)
{
    public sealed record InvalidParams(string Message)
        : Error("invalid_params", Message);

    public sealed record Upstream(string Message, int? StatusCode = null, int? RetryAfterSeconds = null)
        : Error("upstream_error", Message);

    public sealed record Timeout(string Message)
        : Error("timeout", Message);

    public sealed record Unexpected(string Message)
        : Error("unexpected", Message);
}

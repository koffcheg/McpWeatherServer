using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace McpWeatherServer.Infrastructure;

public static class LoggerScopeTiming
{
    public static IDisposable BeginTimedScope(
        this ILogger logger,
        string operationName,
        IReadOnlyDictionary<string, object?>? properties = null,
        LogLevel endLevel = LogLevel.Information)
        => new TimedScope(logger, operationName, properties, endLevel);

    private sealed class TimedScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly LogLevel _endLevel;
        private readonly Stopwatch _sw;
        private readonly IDisposable _scope;

        public TimedScope(
            ILogger logger,
            string operationName,
            IReadOnlyDictionary<string, object?>? properties,
            LogLevel endLevel)
        {
            _logger = logger;
            _operationName = operationName;
            _endLevel = endLevel;
            _sw = Stopwatch.StartNew();

            var scopeState = new Dictionary<string, object?>
            {
                ["operation"] = operationName
            };

            if (properties is not null)
            {
                foreach (var kv in properties)
                    scopeState[kv.Key] = kv.Value;
            }

            _scope = _logger.BeginScope(scopeState);
        }

        public void Dispose()
        {
            _sw.Stop();
            _logger.Log(_endLevel,
                "Operation {operation} completed in {duration_ms} ms",
                _operationName,
                _sw.Elapsed.TotalMilliseconds);

            _scope.Dispose();
        }
    }
}

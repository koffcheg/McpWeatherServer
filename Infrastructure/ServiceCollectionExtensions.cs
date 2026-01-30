using McpWeatherServer.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        string clientName,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>(clientName, client =>
        {
            // Logical base address for service discovery scenarios
            client.BaseAddress = new Uri($"https+http://{clientName}");

            // TotalRequestTimeout is authoritative; avoid double timeouts.
            client.Timeout = Timeout.InfiniteTimeSpan;

            configureClient?.Invoke(client);
        })
            .AddServiceDiscovery();

        builder.AddStandardResilienceHandler((sp, options) =>
        {
            var breaker = sp.GetRequiredService<IOptions<CircuitBreakerPolicyOptions>>().Value;
            var retry = sp.GetRequiredService<IOptions<RetryPolicyOptions>>().Value;
            var timeout = sp.GetRequiredService<IOptions<TotalRequestTimeoutPolicyOptions>>().Value;

            options.Retry.MaxRetryAttempts = retry.RetryCount;

            // Your earlier template used SamplingDuration; we keep it exactly as requested.
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(breaker.BreakDurationSeconds);

            options.TotalRequestTimeout.Timeout = timeout.Timeout;
        });

        return builder;
    }
}

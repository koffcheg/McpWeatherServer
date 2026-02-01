using McpWeatherServer.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddResilientHttpClient<TClient, TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientName,
        Action<HttpClient>? configureClient = null)
        where TClient : class
        where TImplementation : class, TClient
    {
        var builder = services.AddHttpClient<TClient, TImplementation>(clientName, client =>
        {
            client.BaseAddress = new Uri($"https+http://{clientName}");

            client.Timeout = Timeout.InfiniteTimeSpan;

            configureClient?.Invoke(client);
        })
            .AddServiceDiscovery();

        builder.AddStandardResilienceHandler(options =>
        {

            var resilienceOptions = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>();

            options.Retry.MaxRetryAttempts = resilienceOptions?.RetryCount ?? 3;

            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(resilienceOptions?.BreakDurationSeconds ?? 30);

            options.TotalRequestTimeout.Timeout = resilienceOptions?.Timeout ?? TimeSpan.FromSeconds(10);
        });

        return builder;
    }
}

using McpWeatherServer.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
            // Logical base address for service discovery scenarios
            client.BaseAddress = new Uri($"https+http://{clientName}");

            // TotalRequestTimeout is authoritative; avoid double timeouts.
            client.Timeout = Timeout.InfiniteTimeSpan;

            configureClient?.Invoke(client);
        })
            .AddServiceDiscovery();

        builder.AddStandardResilienceHandler(options =>
        {

           // var resilienceOptions = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>();

            options.Retry.MaxRetryAttempts = 3;

            // Your earlier template used SamplingDuration; we keep it exactly as requested.
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);

            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
        });

        return builder;
    }
}

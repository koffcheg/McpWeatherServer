using McpWeatherServer.Infrastructure;
using McpWeatherServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

//var configuration = new ConfigurationBuilder()
//            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//            .Build();

// -------------------------
// Serilog file logging + trace/span enrichment
// -------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithSpan() // adds TraceId/SpanId to logs
    .WriteTo.File(
        path: "logs/app-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new Serilog.Formatting.Json.JsonFormatter())
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// -------------------------
// Service discovery core registration
// -------------------------
builder.Services.AddServiceDiscovery();

// -------------------------
// OpenTelemetry (traces + metrics)
// -------------------------
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("mcp-weather"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Console exporter is fine for local debugging; keep OTLP for later backends.
        .AddConsoleExporter())
    .WithMetrics(m => m
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("mcp-weather"))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// -------------------------
// Typed+Named resilient HttpClient using your template (NO Polly)
// -------------------------
builder.Services.AddResilientHttpClient<IOpenMeteoApiClient, OpenMeteoApiClient>(
    builder.Configuration,
    clientName: ConfigurationConstants.ServiceDiscoveryClientName,
    configureClient: client =>
    {
        // External API: override logical https+http://{clientName} with real endpoint
        client.BaseAddress = new Uri("https://api.open-meteo.com/");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("mcp-weather/1.0");
    });

// -------------------------
// MCP server
// -------------------------
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

// -------------------------
// Middleware pipeline
// (order matters: exception handling should be early)
// -------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMcp("/mcp");

app.Run();

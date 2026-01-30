using McpWeatherServer.Infrastructure;
using McpWeatherServer.Services;

var builder = WebApplication.CreateBuilder(args);

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
// Options binding (from appsettings.json)
// -------------------------
builder.Services.AddOptions<CircuitBreakerPolicyOptions>()
    .Bind(builder.Configuration.GetSection(CircuitBreakerPolicyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<RetryPolicyOptions>()
    .Bind(builder.Configuration.GetSection(RetryPolicyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<TotalRequestTimeoutPolicyOptions>()
    .Bind(builder.Configuration.GetSection(TotalRequestTimeoutPolicyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
    .WithHttpTransport()
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

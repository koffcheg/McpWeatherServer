using McpWeatherServer.Domain;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace McpWeatherServer.Services;

public interface IOpenMeteoApiClient
{
    Task<Result<TodayWeatherResult>> GetTodayAsync(double latitude, double longitude, string timezone, CancellationToken ct);
}

public sealed class OpenMeteoApiClient(HttpClient http) : IOpenMeteoApiClient
{
    public async Task<Result<TodayWeatherResult>> GetTodayAsync(
        double latitude,
        double longitude,
        string timezone,
        CancellationToken ct)
    {
        var path =
            "v1/forecast" +
            $"?latitude={latitude}&longitude={longitude}" +
            "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
            $"&timezone={Uri.EscapeDataString(timezone)}";

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result<TodayWeatherResult>.Fail(new Error.Timeout("Weather provider request timed out."));
        }
        catch
        {
            return Result<TodayWeatherResult>.Fail(new Error.Upstream("Weather provider request failed."));
        }

        if (res.StatusCode == (HttpStatusCode)429)
        {
            int? retry = null;
            if (res.Headers.RetryAfter?.Delta is { } d) retry = (int)d.TotalSeconds;

            return Result<TodayWeatherResult>.Fail(new Error.Upstream(
                "Weather provider rate-limited the request.",
                StatusCode: 429,
                RetryAfterSeconds: retry));
        }

        if (!res.IsSuccessStatusCode)
        {
            return Result<TodayWeatherResult>.Fail(new Error.Upstream(
                "Weather provider returned a non-success response.",
                StatusCode: (int)res.StatusCode));
        }

        OpenMeteoResponse? resp;
        try
        {
            resp = await res.Content.ReadFromJsonAsync<OpenMeteoResponse>(cancellationToken: ct);
        }
        catch
        {
            return Result<TodayWeatherResult>.Fail(new Error.Upstream("Weather provider returned invalid JSON."));
        }

        if (resp?.Daily?.Time is null || resp.Daily.Time.Length == 0)
            return Result<TodayWeatherResult>.Fail(new Error.Upstream("Weather provider returned empty forecast data."));

        return Result<TodayWeatherResult>.Ok(new TodayWeatherResult(
            Date: resp.Daily.Time[0],
            TemperatureMinC: resp.Daily.TemperatureMinC?.ElementAtOrDefault(0),
            TemperatureMaxC: resp.Daily.TemperatureMaxC?.ElementAtOrDefault(0),
            PrecipitationSumMm: resp.Daily.PrecipitationSumMm?.ElementAtOrDefault(0),
            WindSpeedMaxKmh: resp.Daily.WindSpeedMaxKmh?.ElementAtOrDefault(0),
            Timezone: resp.Timezone));
    }

    private sealed class OpenMeteoResponse
    {
        [JsonPropertyName("timezone")] public string? Timezone { get; init; }
        [JsonPropertyName("daily")] public DailyBlock? Daily { get; init; }
    }

    private sealed class DailyBlock
    {
        [JsonPropertyName("time")] public string[]? Time { get; init; }
        [JsonPropertyName("temperature_2m_min")] public double[]? TemperatureMinC { get; init; }
        [JsonPropertyName("temperature_2m_max")] public double[]? TemperatureMaxC { get; init; }
        [JsonPropertyName("precipitation_sum")] public double[]? PrecipitationSumMm { get; init; }
        [JsonPropertyName("wind_speed_10m_max")] public double[]? WindSpeedMaxKmh { get; init; }
    }
}

public sealed record TodayWeatherResult(
    string Date,
    double? TemperatureMinC,
    double? TemperatureMaxC,
    double? PrecipitationSumMm,
    double? WindSpeedMaxKmh,
    string? Timezone
);

namespace McpWeatherServer.Domain;

public sealed record WeatherRequest(double Latitude, double Longitude, string Timezone)
{
    public static Result<WeatherRequest> Create(double lat, double lon, string tz)
    {
        if (lat is < -90 or > 90)
            return Result<WeatherRequest>.Fail(new Error.InvalidParams("latitude must be between -90 and 90"));

        if (lon is < -180 or > 180)
            return Result<WeatherRequest>.Fail(new Error.InvalidParams("longitude must be between -180 and 180"));

        if (string.IsNullOrWhiteSpace(tz) || tz.Length > 64)
            return Result<WeatherRequest>.Fail(new Error.InvalidParams("timezone must be a non-empty IANA timezone"));

        return Result<WeatherRequest>.Ok(new WeatherRequest(lat, lon, tz.Trim()));
    }
}

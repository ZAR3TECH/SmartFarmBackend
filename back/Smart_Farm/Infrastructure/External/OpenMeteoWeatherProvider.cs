using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Smart_Farm.Application.Abstractions;

namespace Smart_Farm.Infrastructure.External;

public class OpenMeteoWeatherProvider : IWeatherProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenMeteoWeatherProvider> _logger;

    // Absolute fallback used only when cache is also empty (first-ever call failure).
    private static readonly DailyWeather _hardcodedFallback =
        new(DateOnly.MinValue, 18, 30, 0, 55, 2.0);

    public OpenMeteoWeatherProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<OpenMeteoWeatherProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DailyWeather> GetDailyAsync(
        double latitude,
        double longitude,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);
        var d   = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Cache key: rounded to 2 decimal places so nearby points share the same entry.
        var cacheKey = $"wx:{Math.Round(latitude, 2)}:{Math.Round(longitude, 2)}";

        var url = $"https://api.open-meteo.com/v1/forecast"
                + $"?latitude={lat}&longitude={lon}"
                + $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,relative_humidity_2m_mean,wind_speed_10m_max"
                + $"&timezone=auto&start_date={d}&end_date={d}";

        var client = _httpClientFactory.CreateClient("OpenMeteo");
        client.Timeout = TimeSpan.FromSeconds(20);

        try
        {
            using var resp = await client.GetAsync(url, cancellationToken);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var daily = doc.RootElement.GetProperty("daily");

            double tmax     = FirstNumber(daily, "temperature_2m_max")          ?? 25;
            double tmin     = FirstNumber(daily, "temperature_2m_min")          ?? 15;
            double rain     = FirstNumber(daily, "precipitation_sum")           ?? 0;
            double? rh      = FirstNumber(daily, "relative_humidity_2m_mean");
            double? windKmh = FirstNumber(daily, "wind_speed_10m_max");
            double? windMps = windKmh.HasValue ? windKmh.Value / 3.6 : (double?)null;

            var result = new DailyWeather(date, tmin, tmax, rain, rh, windMps);

            // Store successful result — expire after 26 hours (covers tomorrow's morning run).
            _cache.Set(cacheKey, result, TimeSpan.FromHours(26));

            return result;
        }
        catch (Exception ex)
        {
            // Try to return last known values for this location.
            if (_cache.TryGetValue(cacheKey, out DailyWeather? cached) && cached is not null)
            {
                _logger.LogWarning(ex,
                    "Open-Meteo fetch failed for {Lat},{Lon} on {Date}. Using last cached values from {CachedDate}.",
                    lat, lon, d, cached.Date);
                return cached with { Date = date, Rain_mm = 0 }; // reset rain — don't assume it'll rain again
            }

            _logger.LogWarning(ex,
                "Open-Meteo fetch failed for {Lat},{Lon} on {Date}. No cache available — using hardcoded fallback.",
                lat, lon, d);
            return _hardcodedFallback with { Date = date };
        }
    }

    private static double? FirstNumber(JsonElement daily, string key)
    {
        if (!daily.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            return null;

        var first = arr[0];
        if (first.ValueKind == JsonValueKind.Number && first.TryGetDouble(out var d))
            return d;
        return null;
    }
}

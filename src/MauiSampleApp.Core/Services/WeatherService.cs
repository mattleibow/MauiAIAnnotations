using System.Globalization;
using System.Text.Json;
using MauiSampleApp.Core.Models;

namespace MauiSampleApp.Core.Services;

public class WeatherService(HttpClient httpClient)
{
    /// <summary>
    /// Gets a 7-day weather forecast for the given coordinates.
    /// </summary>
    public async Task<List<WeatherDay>> GetWeatherByCoordinatesAsync(double latitude, double longitude)
    {
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString("F4", CultureInfo.InvariantCulture)}&longitude={longitude.ToString("F4", CultureInfo.InvariantCulture)}&daily=temperature_2m_mean,weather_code&timezone=auto";

        var response = await httpClient.GetStringAsync(url);
        var forecast = JsonSerializer.Deserialize<WeatherForecast>(response);

        if (forecast?.Daily == null || forecast.Daily.Time.Count == 0)
            return [];

        var days = new List<WeatherDay>();
        for (int i = 0; i < forecast.Daily.Time.Count; i++)
        {
            if (i >= forecast.Daily.TemperatureMean.Count || i >= forecast.Daily.WeatherCode.Count)
                break;

            var date = DateOnly.Parse(forecast.Daily.Time[i]);
            var temp = forecast.Daily.TemperatureMean[i];
            var code = forecast.Daily.WeatherCode[i];

            days.Add(new WeatherDay(
                date,
                temp,
                code,
                WeatherCodeExtensions.GetWeatherEmoji(code),
                WeatherCodeExtensions.GetWeatherDescription(code)));
        }

        return days;
    }

    /// <summary>
    /// Gets the weather summary for a specific date at the given coordinates.
    /// </summary>
    public async Task<string> GetWeatherForecastAsync(double latitude, double longitude, DateOnly date)
    {
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString("F4", CultureInfo.InvariantCulture)}&longitude={longitude.ToString("F4", CultureInfo.InvariantCulture)}&daily=temperature_2m_mean,weather_code&timezone=auto";

            var response = await httpClient.GetStringAsync(url);
            var forecast = JsonSerializer.Deserialize<WeatherForecast>(response);

            if (forecast?.Daily == null || forecast.Daily.Time.Count == 0)
                return "☁️ Weather unavailable";

            var dateString = date.ToString("yyyy-MM-dd");
            var index = forecast.Daily.Time.IndexOf(dateString);

            if (index < 0 || index >= forecast.Daily.TemperatureMean.Count)
                return "☁️ Weather unavailable";

            var temp = forecast.Daily.TemperatureMean[index];
            var weatherCode = forecast.Daily.WeatherCode[index];
            var emoji = WeatherCodeExtensions.GetWeatherEmoji(weatherCode);
            var description = WeatherCodeExtensions.GetWeatherDescription(weatherCode);

            return $"{emoji} {temp:F0}°C - {description}";
        }
        catch
        {
            return "☁️ Weather unavailable";
        }
    }

    /// <summary>
    /// Gets a 7-day weather forecast for a location string (requires geocoding).
    /// The caller must provide coordinates resolved from the location.
    /// </summary>
    public async Task<string> GetWeatherByLocationAsync(string location, Func<string, Task<(double Lat, double Lon)?>> geocodeFunc)
    {
        var coords = await geocodeFunc(location);
        if (coords is null)
            return $"☁️ Could not find location: {location}";

        var days = await GetWeatherByCoordinatesAsync(coords.Value.Lat, coords.Value.Lon);
        if (days.Count == 0)
            return "☁️ Weather unavailable";

        return string.Join("\n", days.Select(d =>
            $"{d.Date:ddd MMM dd}: {d.Emoji} {d.Temperature:F0}°C - {d.Description}"));
    }
}

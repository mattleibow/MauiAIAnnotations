using System.Globalization;
using System.Text.Json;
using MauiSampleApp.Core.Models;

namespace MauiSampleApp.Core.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DailyWeatherItem>> GetWeatherForecastAsync(double latitude, double longitude)
    {
        var lat = latitude.ToString("F4", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("F4", CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&daily=temperature_2m_mean,weather_code&timezone=auto";

        var response = await _httpClient.GetStringAsync(url);
        var forecast = JsonSerializer.Deserialize<WeatherForecast>(response);

        if (forecast?.Daily == null || forecast.Daily.Time.Count == 0)
            return [];

        var items = new List<DailyWeatherItem>();
        for (int i = 0; i < forecast.Daily.Time.Count; i++)
        {
            if (i >= forecast.Daily.TemperatureMean.Count || i >= forecast.Daily.WeatherCode.Count)
                break;

            items.Add(new DailyWeatherItem(
                forecast.Daily.Time[i],
                forecast.Daily.TemperatureMean[i],
                forecast.Daily.WeatherCode[i]));
        }

        return items;
    }

    public async Task<List<DailyWeatherItem>> GetWeatherForecastAsync(string location)
    {
        var geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";

        var geocodingResponse = await _httpClient.GetStringAsync(geocodingUrl);
        var geocoding = JsonSerializer.Deserialize<GeocodingResponse>(geocodingResponse);

        if (geocoding?.Results == null || geocoding.Results.Count == 0)
            return [];

        var result = geocoding.Results[0];
        return await GetWeatherForecastAsync(result.Latitude, result.Longitude);
    }
}

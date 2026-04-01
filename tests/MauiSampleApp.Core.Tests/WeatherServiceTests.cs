using System.Net;
using System.Text.Json;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.Core.Tests;

public class WeatherServiceTests
{
    private static readonly string SampleResponse = JsonSerializer.Serialize(new WeatherForecast
    {
        Latitude = 47.6062,
        Longitude = -122.3321,
        Daily = new DailyWeather
        {
            Time = ["2026-04-01", "2026-04-02", "2026-04-03", "2026-04-04", "2026-04-05", "2026-04-06", "2026-04-07"],
            TemperatureMean = [12.5, 14.0, 10.3, 8.7, 15.2, 16.1, 11.9],
            WeatherCode = [0, 1, 3, 61, 2, 0, 45]
        }
    });

    private static HttpClient CreateMockHttpClient(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseContent, statusCode);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com") };
    }

    [Fact]
    public async Task GetWeatherByCoordinatesAsync_ReturnsWeatherDays()
    {
        var httpClient = CreateMockHttpClient(SampleResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherByCoordinatesAsync(47.6062, -122.3321);

        Assert.NotEmpty(result);
        Assert.Equal(7, result.Count);
        Assert.Equal(new DateOnly(2026, 4, 1), result[0].Date);
        Assert.Equal(12.5, result[0].Temperature);
        Assert.Equal("☀️", result[0].Emoji);
        Assert.Equal("Clear sky", result[0].Description);
    }

    [Fact]
    public async Task GetWeatherByCoordinatesAsync_EmptyResponse_ReturnsEmptyList()
    {
        var emptyResponse = JsonSerializer.Serialize(new WeatherForecast
        {
            Latitude = 0,
            Longitude = 0,
            Daily = new DailyWeather()
        });
        var httpClient = CreateMockHttpClient(emptyResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherByCoordinatesAsync(0, 0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_ReturnsFormattedString()
    {
        var httpClient = CreateMockHttpClient(SampleResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherForecastAsync(47.6062, -122.3321, new DateOnly(2026, 4, 1));

        Assert.Contains("☀️", result);
        Assert.Contains("12°C", result); // 12.5 with F0 format
        Assert.Contains("Clear sky", result);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_DateNotFound_ReturnsUnavailable()
    {
        var httpClient = CreateMockHttpClient(SampleResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherForecastAsync(47.6062, -122.3321, new DateOnly(2026, 12, 25));

        Assert.Equal("☁️ Weather unavailable", result);
    }

    [Fact]
    public async Task GetWeatherByLocationAsync_WithValidGeocode_ReturnsWeather()
    {
        var httpClient = CreateMockHttpClient(SampleResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherByLocationAsync("Seattle",
            _ => Task.FromResult<(double Lat, double Lon)?>(( 47.6062, -122.3321)));

        Assert.Contains("☀️", result);
        Assert.Contains("Clear sky", result);
    }

    [Fact]
    public async Task GetWeatherByLocationAsync_WithNullGeocode_ReturnsNotFound()
    {
        var httpClient = CreateMockHttpClient(SampleResponse);
        var service = new WeatherService(httpClient);

        var result = await service.GetWeatherByLocationAsync("Narnia",
            _ => Task.FromResult<(double Lat, double Lon)?>(null));

        Assert.Contains("Could not find location", result);
    }
}

public class WeatherCodeExtensionsTests
{
    [Theory]
    [InlineData(0, "☀️")]
    [InlineData(1, "🌤️")]
    [InlineData(3, "☁️")]
    [InlineData(61, "🌧️")]
    [InlineData(71, "❄️")]
    [InlineData(95, "⛈️")]
    [InlineData(999, "☁️")]
    public void GetWeatherEmoji_ReturnsCorrectEmoji(int code, string expectedEmoji)
    {
        Assert.Equal(expectedEmoji, WeatherCodeExtensions.GetWeatherEmoji(code));
    }

    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(3, "Overcast")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(999, "Unknown")]
    public void GetWeatherDescription_ReturnsCorrectDescription(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodeExtensions.GetWeatherDescription(code));
    }
}

/// <summary>
/// Simple mock HTTP handler for testing.
/// </summary>
file class MockHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        });
    }
}

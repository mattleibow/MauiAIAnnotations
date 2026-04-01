using System.Net;
using System.Net.Http;
using Moq;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.Core.Tests;

public class WeatherServiceTests
{
    private static HttpClient CreateHttpClient(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new MockHttpMessageHandler(responseContent, statusCode);
        return new HttpClient(handler);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithCoordinates_ReturnsItems()
    {
        var json = """
            {
                "latitude": 51.5074,
                "longitude": -0.1278,
                "daily": {
                    "time": ["2024-01-01", "2024-01-02", "2024-01-03"],
                    "temperature_2m_mean": [5.2, 6.1, 4.8],
                    "weather_code": [0, 3, 61]
                }
            }
            """;

        var service = new WeatherService(CreateHttpClient(json));
        var items = await service.GetWeatherForecastAsync(51.5074, -0.1278);

        Assert.Equal(3, items.Count);
        Assert.Equal("2024-01-01", items[0].Date);
        Assert.Equal(5.2, items[0].Temperature);
        Assert.Equal(0, items[0].WeatherCode);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithEmptyDaily_ReturnsEmptyList()
    {
        var json = """
            {
                "latitude": 0.0,
                "longitude": 0.0,
                "daily": {
                    "time": [],
                    "temperature_2m_mean": [],
                    "weather_code": []
                }
            }
            """;

        var service = new WeatherService(CreateHttpClient(json));
        var items = await service.GetWeatherForecastAsync(0.0, 0.0);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithLocation_CallsGeocodingAndWeather()
    {
        var geocodingJson = """
            {
                "results": [
                    { "latitude": 51.5074, "longitude": -0.1278, "name": "London", "country": "United Kingdom" }
                ]
            }
            """;
        var weatherJson = """
            {
                "latitude": 51.5074,
                "longitude": -0.1278,
                "daily": {
                    "time": ["2024-01-01"],
                    "temperature_2m_mean": [8.0],
                    "weather_code": [1]
                }
            }
            """;

        var handler = new SequentialMockHttpMessageHandler([geocodingJson, weatherJson]);
        var service = new WeatherService(new HttpClient(handler));

        var items = await service.GetWeatherForecastAsync("London");

        Assert.Single(items);
        Assert.Equal("2024-01-01", items[0].Date);
    }

    [Fact]
    public async Task GetWeatherForecastAsync_WithLocationNoResults_ReturnsEmptyList()
    {
        var geocodingJson = """{ "results": [] }""";

        var service = new WeatherService(CreateHttpClient(geocodingJson));
        var items = await service.GetWeatherForecastAsync("NonExistentPlace12345");

        Assert.Empty(items);
    }

    [Fact]
    public void DailyWeatherItem_Emoji_ReturnsCorrectEmoji()
    {
        var item = new DailyWeatherItem("2024-01-01", 10.0, 0);
        Assert.Equal("☀️", item.Emoji);
    }

    [Fact]
    public void DailyWeatherItem_Description_ReturnsCorrectDescription()
    {
        var item = new DailyWeatherItem("2024-01-01", 10.0, 0);
        Assert.Equal("Clear sky", item.Description);
    }
}

public class WeatherCodeExtensionsTests
{
    [Theory]
    [InlineData(0, "☀️")]
    [InlineData(1, "🌤️")]
    [InlineData(2, "🌤️")]
    [InlineData(3, "☁️")]
    [InlineData(45, "🌫️")]
    [InlineData(61, "🌧️")]
    [InlineData(71, "❄️")]
    [InlineData(95, "⛈️")]
    [InlineData(999, "☁️")]
    public void GetWeatherEmoji_ReturnsExpectedEmoji(int code, string expectedEmoji)
    {
        Assert.Equal(expectedEmoji, WeatherCodeExtensions.GetWeatherEmoji(code));
    }

    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(1, "Mainly clear")]
    [InlineData(2, "Partly cloudy")]
    [InlineData(3, "Overcast")]
    [InlineData(45, "Fog")]
    [InlineData(61, "Slight rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(999, "Unknown")]
    public void GetWeatherDescription_ReturnsExpectedDescription(int code, string expectedDescription)
    {
        Assert.Equal(expectedDescription, WeatherCodeExtensions.GetWeatherDescription(code));
    }
}

// Test helpers
public class MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent)
        };
        return Task.FromResult(response);
    }
}

public class SequentialMockHttpMessageHandler(IEnumerable<string> responses) : HttpMessageHandler
{
    private readonly Queue<string> _responses = new(responses);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = _responses.Count > 0 ? _responses.Dequeue() : "{}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };
        return Task.FromResult(response);
    }
}

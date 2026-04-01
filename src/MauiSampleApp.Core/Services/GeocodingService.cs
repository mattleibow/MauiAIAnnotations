using System.Text.Json;
using System.Text.Json.Serialization;

namespace MauiSampleApp.Core.Services;

/// <summary>
/// Uses the free Open-Meteo Geocoding API instead of platform-specific geocoding.
/// Works on all platforms without API keys.
/// </summary>
public class GeocodingService(HttpClient httpClient)
{
    public async Task<(double Latitude, double Longitude)?> GeocodeAsync(string location)
    {
        try
        {
            var encoded = Uri.EscapeDataString(location);
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={encoded}&count=1&language=en&format=json";
            var response = await httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<GeocodeResponse>(response);

            if (result?.Results is null || result.Results.Count == 0)
                return null;

            var first = result.Results[0];
            return (first.Latitude, first.Longitude);
        }
        catch
        {
            return null;
        }
    }
}

internal record GeocodeResponse
{
    [JsonPropertyName("results")]
    public List<GeocodeResult>? Results { get; init; }
}

internal record GeocodeResult
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";
}

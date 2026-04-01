using System.Text.Json.Serialization;

namespace MauiSampleApp.Core.Models;

public record GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult> Results { get; init; } = [];
}

public record GeocodingResult
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; init; } = string.Empty;
}

namespace MauiSampleApp.Core.Models;

/// <summary>
/// Represents a single day of weather for display purposes.
/// </summary>
public record WeatherDay(
    DateOnly Date,
    double Temperature,
    int WeatherCode,
    string Emoji,
    string Description);

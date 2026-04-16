namespace AnnotationsSampleApp.Models;

/// <summary>
/// A plant species in the catalog.
/// </summary>
public record PlantInfo(
    string CommonName,
    string ScientificName,
    string Category,
    string SunlightNeeds,
    int WateringFrequencyDays);

/// <summary>
/// Detailed care guide for a species.
/// </summary>
public record CareGuide(
    string Species,
    string WateringInstructions,
    string SunlightRequirements,
    string SoilType,
    string FertilizingSchedule,
    bool FrostTolerant,
    string[] CommonIssues);

/// <summary>
/// A plant in the user's garden.
/// </summary>
public record PlantEntry(
    string Nickname,
    string Species,
    string Location,
    DateTime AddedAt,
    DateTime? LastWatered = null);

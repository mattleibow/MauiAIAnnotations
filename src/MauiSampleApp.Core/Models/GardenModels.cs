namespace MauiSampleApp.Core.Models;

public class SpeciesProfile
{
    public string Id { get; set; } = string.Empty;
    public string CommonName { get; set; } = string.Empty;
    public string ScientificName { get; set; } = string.Empty;
    public int WateringFrequencyDays { get; set; }
    public string SunlightNeeds { get; set; } = "Medium"; // Low, Medium, Full
    public bool FrostTolerant { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class Plant
{
    public string Id { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string SpeciesId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsIndoor { get; set; }
    public DateTime DateAdded { get; set; }
}

public class CareEvent
{
    public string Id { get; set; } = string.Empty;
    public string PlantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty; // Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed
    public string Notes { get; set; } = string.Empty;
}

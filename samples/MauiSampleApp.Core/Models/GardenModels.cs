using System.ComponentModel;

namespace MauiSampleApp.Core.Models;

[Description("A plant species profile containing care requirements and botanical information.")]
public class SpeciesProfile
{
    [Description("Unique identifier for the species profile.")]
    public string Id { get; set; } = string.Empty;

    [Description("The common name of the plant species (e.g., 'Tomato', 'Basil', 'Rose').")]
    public string CommonName { get; set; } = string.Empty;

    [Description("The scientific/Latin name of the species (e.g., 'Solanum lycopersicum').")]
    public string ScientificName { get; set; } = string.Empty;

    [Description("How often the plant should be watered, in days between waterings.")]
    public int WateringFrequencyDays { get; set; }

    [Description("The plant's sunlight requirements. Must be one of: 'Low', 'Medium', or 'Full'.")]
    public string SunlightNeeds { get; set; } = "Medium";

    [Description("Whether the plant can tolerate frost/freezing temperatures.")]
    public bool FrostTolerant { get; set; }

    [Description("General care tips and notes about growing this species.")]
    public string Notes { get; set; } = string.Empty;
}

[Description("A plant owned by the user, tracked in their garden.")]
public class Plant
{
    [Description("Unique identifier for the plant.")]
    public string Id { get; set; } = string.Empty;

    [Description("A friendly name the user gave this plant (e.g., 'My Tomatoes', 'Basil Buddy').")]
    public string Nickname { get; set; } = string.Empty;

    [Description("The ID of the SpeciesProfile this plant belongs to.")]
    public string SpeciesId { get; set; } = string.Empty;

    [Description("Where the plant is located (e.g., 'Back garden', 'Kitchen windowsill').")]
    public string Location { get; set; } = string.Empty;

    [Description("Whether the plant is kept indoors.")]
    public bool IsIndoor { get; set; }

    [Description("The date the plant was added to the garden tracker.")]
    public DateTime DateAdded { get; set; }
}

[Description("A recorded care event for a plant, such as watering or fertilizing.")]
public class CareEvent
{
    [Description("Unique identifier for the care event.")]
    public string Id { get; set; } = string.Empty;

    [Description("The ID of the plant this care event belongs to.")]
    public string PlantId { get; set; } = string.Empty;

    [Description("When the care event occurred.")]
    public DateTime Timestamp { get; set; }

    [Description("The type of care performed. Must be one of: 'Watered', 'Fertilized', 'Pruned', 'Repotted', 'TreatedForPest', 'Observed'.")]
    public string EventType { get; set; } = string.Empty;

    [Description("Optional notes about the care event.")]
    public string Notes { get; set; } = string.Empty;
}

// Request types for AI tool arguments

[Description("Request to add a new plant to the garden.")]
public class NewPlantRequest
{
    [Description("A friendly name for the plant (e.g. 'My Tomatoes', 'Basil Buddy').")]
    public string Nickname { get; set; } = "";

    [Description("The species name (e.g. 'tomato', 'basil', 'rose').")]
    public string Species { get; set; } = "";

    [Description("Where the plant is located (e.g. 'Back garden', 'Kitchen windowsill').")]
    public string Location { get; set; } = "";

    [Description("Whether the plant is kept indoors.")]
    public bool IsIndoor { get; set; }
}

[Description("A care event to log for a plant.")]
public class CareEventRequest
{
    [Description("The type of care performed. Must be one of: Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed, Mulched, Weeded.")]
    public string EventType { get; set; } = "";

    [Description("Optional notes about the care event.")]
    public string Notes { get; set; } = "";
}

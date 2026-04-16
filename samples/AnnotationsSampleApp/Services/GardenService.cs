using System.ComponentModel;
using AnnotationsSampleApp.Models;
using Microsoft.Extensions.AI.Attributes;

namespace AnnotationsSampleApp.Services;

/// <summary>
/// Manages the user's garden for the current session.
/// Registered as scoped — each DI scope (chat session) gets its own instance
/// with isolated state. Creating a new scope resets the garden.
/// </summary>
public class GardenService
{
    private readonly List<PlantEntry> _garden = [];

    [Description("Lists all plants currently in the user's garden for this session.")]
    [ExportAIFunction("list_my_garden")]
    public List<PlantEntry> ListMyGarden()
    {
        return [.. _garden];
    }

    [Description("Adds a new plant to the user's garden.")]
    [ExportAIFunction("add_to_garden")]
    public PlantEntry AddToGarden(
        [Description("A friendly nickname for the plant (e.g., 'Kitchen Basil')")] string nickname,
        [Description("The species of the plant (e.g., 'basil', 'tomato')")] string species,
        [Description("Where the plant is located (e.g., 'kitchen windowsill', 'backyard')")] string location)
    {
        var existing = _garden.FirstOrDefault(p =>
            string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            throw new InvalidOperationException($"A plant named '{nickname}' already exists in your garden.");

        var entry = new PlantEntry(nickname, species, location, DateTime.Now);
        _garden.Add(entry);
        return entry;
    }

    [Description("Removes a plant from the user's garden by nickname.")]
    [ExportAIFunction("remove_from_garden")]
    public bool RemoveFromGarden(
        [Description("The nickname of the plant to remove")] string nickname)
    {
        var plant = _garden.FirstOrDefault(p =>
            string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

        if (plant is null)
            return false;

        _garden.Remove(plant);
        return true;
    }

    [Description("Waters a plant in the garden, updating its last-watered timestamp.")]
    [ExportAIFunction("water_plant")]
    public string WaterPlant(
        [Description("The nickname of the plant to water")] string nickname)
    {
        var index = _garden.FindIndex(p =>
            string.Equals(p.Nickname, nickname, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return $"No plant named '{nickname}' found in your garden.";

        _garden[index] = _garden[index] with { LastWatered = DateTime.Now };
        return $"Watered '{nickname}' at {DateTime.Now:h:mm tt}.";
    }
}

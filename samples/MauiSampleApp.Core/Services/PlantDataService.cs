using System.ComponentModel;
using MauiAIAnnotations;
using MauiSampleApp.Core.Models;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class PlantDataService(IDocumentStore store, SpeciesService speciesService)
{
    [ExportAIFunction("get_plants", Description = "Gets all plants the user has registered.")]
    public async Task<List<Plant>> GetPlantsAsync()
    {
        var result = await store.Query<Plant>().ToList();
        return result.ToList();
    }

    [ExportAIFunction("get_plant", Description = "Gets a specific plant by its nickname.")]
    public async Task<Plant?> GetPlantAsync(
        [Description("The nickname of the plant to look up")] string nickname)
    {
        var all = await store.Query<Plant>().ToList();
        return all.FirstOrDefault(p => p.Nickname.Equals(nickname.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    [ExportAIFunction("add_plant", Description = "Adds a new plant. Requires nickname, species name, location, and whether it's indoors.")]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name for the plant")] string nickname,
        [Description("The species name (e.g. 'tomato', 'basil')")] string species,
        [Description("Where the plant is located (e.g. 'Back garden', 'Kitchen windowsill')")] string location,
        [Description("Whether the plant is kept indoors")] bool isIndoor)
    {
        var speciesProfile = await speciesService.GetSpeciesAsync(species);

        var plant = new Plant
        {
            Id = Guid.NewGuid().ToString(),
            Nickname = nickname,
            SpeciesId = speciesProfile.Id,
            Location = location,
            IsIndoor = isIndoor,
            DateAdded = DateTime.UtcNow
        };

        await store.Insert(plant);
        return plant;
    }

    [ExportAIFunction("remove_plant", Description = "Removes a plant by its nickname.")]
    public async Task RemovePlantAsync(
        [Description("The nickname of the plant to remove")] string nickname)
    {
        var plant = await GetPlantAsync(nickname);
        if (plant is not null)
        {
            await store.Remove<Plant>(plant.Id);
        }
    }

    [ExportAIFunction("log_care_event", Description = "Logs a care event for a plant. EventType must be one of: Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed.")]
    public async Task<CareEvent> LogCareEventAsync(
        [Description("The nickname of the plant")] string plantNickname,
        [Description("The type of care performed (Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed)")] string eventType,
        [Description("Optional notes about the care event")] string notes)
    {
        var plant = await GetPlantAsync(plantNickname)
            ?? throw new InvalidOperationException($"Plant '{plantNickname}' not found.");

        var careEvent = new CareEvent
        {
            Id = Guid.NewGuid().ToString(),
            PlantId = plant.Id,
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            Notes = notes
        };

        await store.Insert(careEvent);
        return careEvent;
    }

    [ExportAIFunction("get_care_history", Description = "Gets the care history for a plant by its nickname.")]
    public async Task<List<CareEvent>> GetCareHistoryAsync(
        [Description("The nickname of the plant")] string plantNickname)
    {
        var plant = await GetPlantAsync(plantNickname);
        if (plant is null)
            return [];

        var all = await store.Query<CareEvent>().ToList();
        return all.Where(e => e.PlantId == plant.Id)
                  .OrderByDescending(e => e.Timestamp)
                  .ToList();
    }
}

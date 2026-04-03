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

    [ExportAIFunction("add_plant", Description = "Adds a new plant to the garden.", ApprovalRequired = true)]
    public async Task<Plant> AddPlantAsync(
        [Description("The plant details to add")] NewPlantRequest request)
    {
        var speciesProfile = await speciesService.GetSpeciesAsync(request.Species);

        var plant = new Plant
        {
            Id = Guid.NewGuid().ToString(),
            Nickname = request.Nickname,
            SpeciesId = speciesProfile.Id,
            Location = request.Location,
            IsIndoor = request.IsIndoor,
            DateAdded = DateTime.UtcNow
        };

        await store.Insert(plant);
        return plant;
    }

    [ExportAIFunction("remove_plant", Description = "Removes a plant by its nickname.", ApprovalRequired = true)]
    public async Task RemovePlantAsync(
        [Description("The nickname of the plant to remove")] string nickname)
    {
        var plant = await GetPlantAsync(nickname);
        if (plant is not null)
        {
            await store.Remove<Plant>(plant.Id);
        }
    }

    [ExportAIFunction("log_care_event", Description = "Logs a care event for a plant.")]
    public async Task<CareEvent> LogCareEventAsync(
        [Description("The nickname of the plant")] string plantNickname,
        [Description("The care event details")] CareEventRequest careEvent)
    {
        var plant = await GetPlantAsync(plantNickname)
            ?? throw new InvalidOperationException($"Plant '{plantNickname}' not found.");

        var result = new CareEvent
        {
            Id = Guid.NewGuid().ToString(),
            PlantId = plant.Id,
            Timestamp = DateTime.UtcNow,
            EventType = careEvent.EventType,
            Notes = careEvent.Notes
        };

        await store.Insert(result);
        return result;
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

    [ExportAIFunction("log_batch_care_events",
        Description = "Log multiple care events for a plant at once. Use when the user reports several activities.",
        ApprovalRequired = true)]
    public async Task<List<CareEvent>> LogBatchCareEventsAsync(
        [Description("The nickname of the plant")] string plantNickname,
        [Description("The care events to log")] List<CareEventRequest> careEvents)
    {
        var plant = await GetPlantAsync(plantNickname);
        if (plant is null)
            return [];

        var results = new List<CareEvent>();
        foreach (var req in careEvents)
        {
            var careEvent = new CareEvent
            {
                Id = Guid.NewGuid().ToString(),
                PlantId = plant.Id,
                EventType = req.EventType.Trim(),
                Notes = req.Notes,
                Timestamp = DateTime.UtcNow
            };
            await store.Insert(careEvent);
            results.Add(careEvent);
        }
        return results;
    }
}

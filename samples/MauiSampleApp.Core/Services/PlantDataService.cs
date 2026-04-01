using MauiSampleApp.Core.Models;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class PlantDataService(IDocumentStore store, SpeciesService speciesService)
{
    public async Task<List<Plant>> GetPlantsAsync()
    {
        var result = await store.Query<Plant>().ToList();
        return result.ToList();
    }

    public async Task<Plant?> GetPlantAsync(string nickname)
    {
        var all = await store.Query<Plant>().ToList();
        return all.FirstOrDefault(p => p.Nickname.Equals(nickname.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Plant> AddPlantAsync(string nickname, string species, string location, bool isIndoor)
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

    public async Task RemovePlantAsync(string nickname)
    {
        var plant = await GetPlantAsync(nickname);
        if (plant is not null)
        {
            await store.Remove<Plant>(plant.Id);
        }
    }

    public async Task<CareEvent> LogCareEventAsync(string plantNickname, string eventType, string notes)
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

    public async Task<List<CareEvent>> GetCareHistoryAsync(string plantNickname)
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

using MauiSampleApp.Core.Models;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class PlantDataService
{
    private readonly IDocumentStore _store;
    private readonly SpeciesService _speciesService;

    public PlantDataService(IDocumentStore store, SpeciesService speciesService)
    {
        _store = store;
        _speciesService = speciesService;
    }

    public async Task<List<Plant>> GetPlantsAsync()
    {
        var result = await _store.Query<Plant>().ToList();
        return result.ToList();
    }

    public async Task<Plant?> GetPlantAsync(string nickname)
    {
        var all = await _store.Query<Plant>().ToList();
        return all.FirstOrDefault(p => p.Nickname.Equals(nickname.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Plant> AddPlantAsync(string nickname, string species, string location, bool isIndoor)
    {
        var speciesProfile = await _speciesService.GetSpeciesAsync(species);

        var plant = new Plant
        {
            Id = Guid.NewGuid().ToString(),
            Nickname = nickname,
            SpeciesId = speciesProfile.Id,
            Location = location,
            IsIndoor = isIndoor,
            DateAdded = DateTime.UtcNow
        };

        await _store.Insert(plant);
        return plant;
    }

    public async Task RemovePlantAsync(string nickname)
    {
        var plant = await GetPlantAsync(nickname);
        if (plant is not null)
        {
            await _store.Remove<Plant>(plant.Id);
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

        await _store.Insert(careEvent);
        return careEvent;
    }

    public async Task<List<CareEvent>> GetCareHistoryAsync(string plantNickname)
    {
        var plant = await GetPlantAsync(plantNickname);
        if (plant is null)
            return [];

        var all = await _store.Query<CareEvent>().ToList();
        return all.Where(e => e.PlantId == plant.Id)
                  .OrderByDescending(e => e.Timestamp)
                  .ToList();
    }
}

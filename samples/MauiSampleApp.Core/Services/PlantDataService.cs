using System.ComponentModel;
using Microsoft.Extensions.AI.Attributes;
using MauiSampleApp.Core.Models;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class PlantDataService(IDocumentStore store, SpeciesService speciesService)
{
    public event EventHandler? PlantsChanged;

    [Description("Gets the user's plants. Optionally pass a query to filter by species, nickname, or location for requests like 'all my tomatoes' or 'plants on the balcony'.")]
    [ExportAIFunction("get_plants")]
    public async Task<List<Plant>> GetPlantsAsync(
        [Description("Optional search text to filter plants by species, nickname, or location. Leave blank to return all plants.")] string? query = null)
    {
        var plants = (await store.Query<Plant>().ToList()).ToList();
        if (string.IsNullOrWhiteSpace(query))
            return plants;

        var normalizedQuery = query.Trim();
        var speciesMatches = (await store.Query<SpeciesProfile>().ToList())
            .Where(profile =>
                Matches(profile.CommonName, normalizedQuery) ||
                Matches(profile.ScientificName, normalizedQuery))
            .Select(profile => profile.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return plants
            .Where(plant =>
                Matches(plant.Nickname, normalizedQuery) ||
                Matches(plant.Location, normalizedQuery) ||
                speciesMatches.Contains(plant.SpeciesId))
            .ToList();
    }

    [Description("Gets a specific plant by its nickname.")]
    [ExportAIFunction("get_plant")]
    public async Task<Plant?> GetPlantAsync(
        [Description("The nickname of the plant to look up")] string nickname)
    {
        var normalizedNickname = NormalizePlantLookup(nickname);
        if (string.IsNullOrWhiteSpace(normalizedNickname))
        {
            return null;
        }

        var all = await store.Query<Plant>().ToList();
        var exactMatch = all.FirstOrDefault(p =>
            string.Equals(NormalizePlantLookup(p.Nickname), normalizedNickname, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return exactMatch;
        }

        var fallbackMatches = all
            .Where(p =>
            {
                var plantNickname = NormalizePlantLookup(p.Nickname);
                return !string.IsNullOrWhiteSpace(plantNickname) &&
                       normalizedNickname.Contains(plantNickname, StringComparison.OrdinalIgnoreCase);
            })
            .Take(2)
            .ToList();

        return fallbackMatches.Count == 1 ? fallbackMatches[0] : null;
    }

    [Description("Adds a new plant to the garden.")]
    [ExportAIFunction("add_plant", ApprovalRequired = true)]
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
        OnPlantsChanged();
        return plant;
    }

    [Description("Removes a plant by its nickname.")]
    [ExportAIFunction("remove_plant", ApprovalRequired = true)]
    public async Task RemovePlantAsync(
        [Description("The nickname of the plant to remove")] string nickname)
    {
        var plant = await GetPlantAsync(nickname);
        if (plant is not null)
        {
            await store.Remove<Plant>(plant.Id);
            OnPlantsChanged();
        }
    }

    [Description("Logs a care event for a plant.")]
    [ExportAIFunction("log_care_event")]
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
        OnPlantsChanged();
        return result;
    }

    [Description("Gets the care history for a plant by its nickname.")]
    [ExportAIFunction("get_care_history")]
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

    [Description("Log multiple care events for a plant at once. Use when the user reports several activities.")]
    [ExportAIFunction("log_batch_care_events", ApprovalRequired = true)]
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

        if (results.Count > 0)
        {
            OnPlantsChanged();
        }

        return results;
    }

    private static bool Matches(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static string NormalizePlantLookup(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim()
                .Trim('\"', '\'', '“', '”', '‘', '’')
                .TrimEnd('.', ',', '!', '?', ';', ':')
                .Trim();

    private void OnPlantsChanged() => PlantsChanged?.Invoke(this, EventArgs.Empty);
}

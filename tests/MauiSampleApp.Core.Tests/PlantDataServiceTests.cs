using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;

namespace MauiSampleApp.Core.Tests;

public class PlantDataServiceTests : IDisposable
{
    private readonly IDocumentStore _store;
    private readonly PlantDataService _service;
    private readonly SpeciesService _speciesService;

    public PlantDataServiceTests()
    {
        _store = new SqliteDocumentStore("Data Source=:memory:");
        _speciesService = new SpeciesService(_store, new FakeSpeciesChatClient());
        _service = new PlantDataService(_store, _speciesService);
    }

    public void Dispose() => (_store as IDisposable)?.Dispose();

    [Fact]
    public async Task GetPlants_EmptyByDefault()
    {
        var plants = await _service.GetPlantsAsync();
        Assert.Empty(plants);
    }

    [Fact]
    public async Task AddPlant_CreatesPlantAndSpecies()
    {
        var plant = await _service.AddPlantAsync(new NewPlantRequest { Nickname = "My Tomato", Species = "tomato", Location = "Back garden", IsIndoor = false });

        Assert.NotNull(plant);
        Assert.Equal("My Tomato", plant.Nickname);
        Assert.Equal("Back garden", plant.Location);
        Assert.False(plant.IsIndoor);
        Assert.NotEmpty(plant.SpeciesId);
    }

    [Fact]
    public async Task AddPlant_AppearsInGetPlants()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Basil Buddy", Species = "basil", Location = "Kitchen windowsill", IsIndoor = true });

        var plants = await _service.GetPlantsAsync();
        Assert.Single(plants);
        Assert.Equal("Basil Buddy", plants[0].Nickname);
    }

    [Fact]
    public async Task GetPlants_WithQuery_FiltersBySpeciesNicknameAndLocation()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Patio Tomatoes", Species = "tomato", Location = "Patio", IsIndoor = false });
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Kitchen Basil", Species = "basil", Location = "Kitchen", IsIndoor = true });
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Cherry Tomato", Species = "tomato", Location = "Greenhouse", IsIndoor = true });

        var tomatoPlants = await _service.GetPlantsAsync("tomato");
        var patioPlants = await _service.GetPlantsAsync("patio");

        Assert.Equal(2, tomatoPlants.Count);
        Assert.Contains(tomatoPlants, plant => plant.Nickname == "Patio Tomatoes");
        Assert.Contains(tomatoPlants, plant => plant.Nickname == "Cherry Tomato");
        Assert.Single(patioPlants);
        Assert.Equal("Patio Tomatoes", patioPlants[0].Nickname);
    }

    [Fact]
    public async Task GetPlant_FindsByNickname()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Rosie", Species = "rose", Location = "Front garden", IsIndoor = false });

        var plant = await _service.GetPlantAsync("Rosie");
        Assert.NotNull(plant);
        Assert.Equal("Rosie", plant.Nickname);
    }

    [Fact]
    public async Task GetPlant_CaseInsensitive()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Minty", Species = "mint", Location = "Balcony", IsIndoor = false });

        var plant = await _service.GetPlantAsync("minty");
        Assert.NotNull(plant);
        Assert.Equal("Minty", plant.Nickname);
    }

    [Fact]
    public async Task GetPlant_ReturnsNullForMissing()
    {
        var plant = await _service.GetPlantAsync("nonexistent");
        Assert.Null(plant);
    }

    [Fact]
    public async Task RemovePlant_RemovesFromStore()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Temporary", Species = "fern", Location = "Office", IsIndoor = true });
        Assert.Single(await _service.GetPlantsAsync());

        await _service.RemovePlantAsync("Temporary");
        Assert.Empty(await _service.GetPlantsAsync());
    }

    [Fact]
    public async Task RemovePlant_NoOpIfNotFound()
    {
        await _service.RemovePlantAsync("nonexistent");
    }

    [Fact]
    public async Task RemovePlant_TrimsSurroundingPunctuation()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Patio Rose", Species = "rose", Location = "Patio", IsIndoor = false });

        await _service.RemovePlantAsync("\"Patio Rose.\"");

        Assert.Empty(await _service.GetPlantsAsync());
    }

    [Fact]
    public async Task AddAndRemovePlant_RaisePlantsChanged()
    {
        var changes = 0;
        _service.PlantsChanged += (_, _) => changes++;

        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Watcher", Species = "fern", Location = "Desk", IsIndoor = true });
        await _service.RemovePlantAsync("Watcher");

        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task LogCareEvent_CreatesEvent()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Tomatoes", Species = "tomato", Location = "Garden", IsIndoor = false });

        var careEvent = await _service.LogCareEventAsync("Tomatoes", new CareEventRequest { EventType = "Watered", Notes = "Gave a good soak" });

        Assert.NotNull(careEvent);
        Assert.Equal("Watered", careEvent.EventType);
        Assert.Equal("Gave a good soak", careEvent.Notes);
    }

    [Fact]
    public async Task LogCareEvent_ThrowsForMissingPlant()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.LogCareEventAsync("nonexistent", new CareEventRequest { EventType = "Watered", Notes = "" }));
    }

    [Fact]
    public async Task GetCareHistory_ReturnsEventsInOrder()
    {
        await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Herb", Species = "basil", Location = "Kitchen", IsIndoor = true });

        await _service.LogCareEventAsync("Herb", new CareEventRequest { EventType = "Watered", Notes = "Morning water" });
        await Task.Delay(10);
        await _service.LogCareEventAsync("Herb", new CareEventRequest { EventType = "Fertilized", Notes = "Monthly feed" });

        var history = await _service.GetCareHistoryAsync("Herb");

        Assert.Equal(2, history.Count);
        Assert.Equal("Fertilized", history[0].EventType);
        Assert.Equal("Watered", history[1].EventType);
    }

    [Fact]
    public async Task GetCareHistory_ReturnsEmptyForMissingPlant()
    {
        var history = await _service.GetCareHistoryAsync("nonexistent");
        Assert.Empty(history);
    }

    [Fact]
    public async Task AddPlant_MultiplePlantsShareSpecies()
    {
        var plant1 = await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Tom 1", Species = "tomato", Location = "Garden", IsIndoor = false });
        var plant2 = await _service.AddPlantAsync(new NewPlantRequest { Nickname = "Tom 2", Species = "tomato", Location = "Greenhouse", IsIndoor = true });

        Assert.Equal(plant1.SpeciesId, plant2.SpeciesId);
    }
}

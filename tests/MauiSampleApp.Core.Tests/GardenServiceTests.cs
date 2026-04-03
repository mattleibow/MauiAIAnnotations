using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;
using Microsoft.Extensions.AI;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;

namespace MauiSampleApp.Core.Tests;

/// <summary>
/// A simple IChatClient for testing that returns a valid species profile JSON.
/// </summary>
public sealed class FakeSpeciesChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("FakeSpecies");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Extract species name from the prompt
        var lastMessage = messages.LastOrDefault()?.Text ?? "";
        var name = "Unknown";
        var startIdx = lastMessage.IndexOf('"');
        var endIdx = lastMessage.IndexOf('"', startIdx + 1);
        if (startIdx >= 0 && endIdx > startIdx)
            name = lastMessage[(startIdx + 1)..endIdx];

        var capitalName = char.ToUpper(name[0]) + name[1..];
        var json = $$"""
            {
                "CommonName": "{{capitalName}}",
                "ScientificName": "{{capitalName}} testicus",
                "WateringFrequencyDays": 5,
                "SunlightNeeds": "Full",
                "FrostTolerant": false,
                "Notes": "Test notes for {{name}}."
            }
            """;

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, json)]);
        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

public class SpeciesServiceTests : IDisposable
{
    private readonly IDocumentStore _store;
    private readonly SpeciesService _service;

    public SpeciesServiceTests()
    {
        _store = new SqliteDocumentStore("Data Source=:memory:");
        _service = new SpeciesService(_store, new FakeSpeciesChatClient());
    }

    public void Dispose() => (_store as IDisposable)?.Dispose();

    [Fact]
    public async Task GetSpecies_ReturnsProfile()
    {
        var profile = await _service.GetSpeciesAsync("tomato");

        Assert.NotNull(profile);
        Assert.Equal("Tomato", profile.CommonName);
        Assert.NotEmpty(profile.Id);
    }

    [Fact]
    public async Task GetSpecies_ReturnsCachedOnSecondCall()
    {
        var first = await _service.GetSpeciesAsync("basil");
        var second = await _service.GetSpeciesAsync("basil");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.CommonName, second.CommonName);
    }

    [Fact]
    public async Task GetSpecies_CaseInsensitive()
    {
        var lower = await _service.GetSpeciesAsync("mint");
        var upper = await _service.GetSpeciesAsync("MINT");
        var mixed = await _service.GetSpeciesAsync("Mint");

        Assert.Equal(lower.Id, upper.Id);
        Assert.Equal(lower.Id, mixed.Id);
    }

    [Fact]
    public async Task GetSpecies_TrimsWhitespace()
    {
        var first = await _service.GetSpeciesAsync("rosemary");
        var padded = await _service.GetSpeciesAsync("  rosemary  ");

        Assert.Equal(first.Id, padded.Id);
    }

    [Fact]
    public async Task GetSpecies_SetsDefaultFields()
    {
        var profile = await _service.GetSpeciesAsync("lavender");

        Assert.True(profile.WateringFrequencyDays > 0);
        Assert.NotEmpty(profile.SunlightNeeds);
        Assert.NotEmpty(profile.Notes);
        Assert.NotEmpty(profile.ScientificName);
    }
}

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
        await _service.RemovePlantAsync("nonexistent"); // should not throw
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
        await Task.Delay(10); // ensure different timestamps
        await _service.LogCareEventAsync("Herb", new CareEventRequest { EventType = "Fertilized", Notes = "Monthly feed" });

        var history = await _service.GetCareHistoryAsync("Herb");

        Assert.Equal(2, history.Count);
        Assert.Equal("Fertilized", history[0].EventType); // most recent first
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


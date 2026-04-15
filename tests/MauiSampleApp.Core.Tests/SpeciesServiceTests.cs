using MauiSampleApp.Core.Services;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;

namespace MauiSampleApp.Core.Tests;

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

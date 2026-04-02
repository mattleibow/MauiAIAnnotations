using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiSampleApp.Core.Models;
using Shiny.DocumentDb;

namespace MauiSampleApp.ViewModels;

public partial class DebugViewModel(IDocumentStore store) : ObservableObject
{
    public ObservableCollection<string> SpeciesProfiles { get; } = [];
    public ObservableCollection<string> Plants { get; } = [];
    public ObservableCollection<string> CareEvents { get; } = [];

    public async Task LoadDataAsync()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };

        SpeciesProfiles.Clear();
        var species = await store.Query<SpeciesProfile>().ToList();
        foreach (var s in species)
            SpeciesProfiles.Add(JsonSerializer.Serialize(s, options));

        Plants.Clear();
        var plants = await store.Query<Plant>().ToList();
        foreach (var p in plants)
            Plants.Add(JsonSerializer.Serialize(p, options));

        CareEvents.Clear();
        var events = await store.Query<CareEvent>().ToList();
        foreach (var e in events)
            CareEvents.Add(JsonSerializer.Serialize(e, options));

        OnPropertyChanged(nameof(SpeciesProfiles));
        OnPropertyChanged(nameof(Plants));
        OnPropertyChanged(nameof(CareEvents));
    }

    public async Task ClearAllDataAsync()
    {
        var species = await store.Query<SpeciesProfile>().ToList();
        foreach (var s in species)
            await store.Remove<SpeciesProfile>(s.Id);

        var plants = await store.Query<Plant>().ToList();
        foreach (var p in plants)
            await store.Remove<Plant>(p.Id);

        var events = await store.Query<CareEvent>().ToList();
        foreach (var e in events)
            await store.Remove<CareEvent>(e.Id);
    }
}

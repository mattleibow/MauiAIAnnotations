using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public partial class PlantDetailViewModel(PlantDataService plantDataService, SpeciesService speciesService) : ObservableObject, IQueryAttributable
{
    public ObservableCollection<CareEvent> CareHistory { get; } = [];

    [ObservableProperty]
    public partial Plant? Plant { get; set; }

    [ObservableProperty]
    public partial SpeciesProfile? Species { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("nickname", out var nickname) && nickname is string name)
        {
            _ = LoadPlantAsync(name);
        }
    }

    private async Task LoadPlantAsync(string nickname)
    {
        IsLoading = true;
        try
        {
            Plant = await plantDataService.GetPlantAsync(nickname);
            if (Plant is not null)
            {
                Species = await speciesService.GetSpeciesByIdAsync(Plant.SpeciesId);

                var history = await plantDataService.GetCareHistoryAsync(nickname);
                CareHistory.Clear();
                foreach (var e in history)
                    CareHistory.Add(e);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LogCareAsync(string eventType)
    {
        if (Plant is null) return;
        await plantDataService.LogCareEventAsync(Plant.Nickname, new() { EventType = eventType });

        var history = await plantDataService.GetCareHistoryAsync(Plant.Nickname);
        CareHistory.Clear();
        foreach (var e in history)
            CareHistory.Add(e);
    }

    [RelayCommand]
    private async Task DeletePlantAsync()
    {
        if (Plant is null) return;
        await plantDataService.RemovePlantAsync(Plant.Nickname);
        await Shell.Current.GoToAsync("..");
    }
}

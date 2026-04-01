using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class PlantDetailViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly PlantDataService _plantDataService;
    private readonly SpeciesService _speciesService;

    public PlantDetailViewModel(PlantDataService plantDataService, SpeciesService speciesService)
    {
        _plantDataService = plantDataService;
        _speciesService = speciesService;
        CareHistory = [];
        LogCareCommand = new Command<string>(async (eventType) => await LogCareAsync(eventType));
        DeletePlantCommand = new Command(async () => await DeletePlantAsync());
    }

    private Plant? _plant;
    public Plant? Plant
    {
        get => _plant;
        set { _plant = value; OnPropertyChanged(); }
    }

    private SpeciesProfile? _species;
    public SpeciesProfile? Species
    {
        get => _species;
        set { _species = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CareEvent> CareHistory { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand LogCareCommand { get; }
    public ICommand DeletePlantCommand { get; }

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
            Plant = await _plantDataService.GetPlantAsync(nickname);
            if (Plant is not null)
            {
                var allSpecies = await _speciesService.GetSpeciesAsync(Plant.SpeciesId);
                Species = allSpecies;

                var history = await _plantDataService.GetCareHistoryAsync(nickname);
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

    private async Task LogCareAsync(string eventType)
    {
        if (Plant is null) return;
        await _plantDataService.LogCareEventAsync(Plant.Nickname, eventType, "");

        var history = await _plantDataService.GetCareHistoryAsync(Plant.Nickname);
        CareHistory.Clear();
        foreach (var e in history)
            CareHistory.Add(e);
    }

    private async Task DeletePlantAsync()
    {
        if (Plant is null) return;
        await _plantDataService.RemovePlantAsync(Plant.Nickname);
        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class PlantDetailViewModel(PlantDataService plantDataService, SpeciesService speciesService) : INotifyPropertyChanged, IQueryAttributable
{
    public ObservableCollection<CareEvent> CareHistory { get; } = [];

    private ChatViewModel? _chatViewModel;
    public ChatViewModel? ChatViewModel
    {
        get => _chatViewModel;
        set { _chatViewModel = value; OnPropertyChanged(); }
    }

    private Command<string>? _logCareCommand;
    public ICommand LogCareCommand => _logCareCommand ??= new Command<string>(async eventType => await LogCareAsync(eventType));

    private Command? _deletePlantCommand;
    public ICommand DeletePlantCommand => _deletePlantCommand ??= new Command(async () => await DeletePlantAsync());

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

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

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

    private async Task LogCareAsync(string eventType)
    {
        if (Plant is null) return;
        await plantDataService.LogCareEventAsync(Plant.Nickname, eventType, "");

        var history = await plantDataService.GetCareHistoryAsync(Plant.Nickname);
        CareHistory.Clear();
        foreach (var e in history)
            CareHistory.Add(e);
    }

    private async Task DeletePlantAsync()
    {
        if (Plant is null) return;
        await plantDataService.RemovePlantAsync(Plant.Nickname);
        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

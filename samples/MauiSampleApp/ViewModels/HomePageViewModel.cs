using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class HomePageViewModel(PlantDataService plantDataService) : INotifyPropertyChanged
{
    public ObservableCollection<Plant> Plants { get; } = [];

    private ChatViewModel? _chatViewModel;
    public ChatViewModel? ChatViewModel
    {
        get => _chatViewModel;
        set { _chatViewModel = value; OnPropertyChanged(); }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; } = new Command(async () => { });

    public async Task RefreshPlantsAsync()
    {
        IsLoading = true;
        try
        {
            var plants = await plantDataService.GetPlantsAsync();
            Plants.Clear();
            foreach (var plant in plants)
                Plants.Add(plant);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public partial class HomePageViewModel(PlantDataService plantDataService) : ObservableObject
{
    public ObservableCollection<Plant> Plants { get; } = [];

    [ObservableProperty]
    public partial GardenChatViewModel? ChatViewModel { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [RelayCommand]
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
}

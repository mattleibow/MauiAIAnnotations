using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;
using Microsoft.Maui.ApplicationModel;

namespace MauiSampleApp.ViewModels;

public partial class HomePageViewModel : ObservableObject, IDisposable
{
    private readonly PlantDataService _plantDataService;

    public HomePageViewModel(PlantDataService plantDataService)
    {
        _plantDataService = plantDataService;
        _plantDataService.PlantsChanged += OnPlantsChanged;
    }

    public ObservableCollection<Plant> Plants { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [RelayCommand]
    public async Task RefreshPlantsAsync()
    {
        IsLoading = true;
        try
        {
            var plants = await _plantDataService.GetPlantsAsync();
            Plants.Clear();
            foreach (var plant in plants)
                Plants.Add(plant);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnPlantsChanged(object? sender, EventArgs e)
    {
        if (IsLoading)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(RefreshPlantsAsync);
    }

    public void Dispose()
    {
        _plantDataService.PlantsChanged -= OnPlantsChanged;
    }
}

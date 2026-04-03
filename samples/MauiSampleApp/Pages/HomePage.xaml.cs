using MauiSampleApp.Core.Models;
using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomePageViewModel _viewModel;

    public HomePage(HomePageViewModel viewModel, GardenChatViewModel chatViewModel)
    {
        _viewModel = viewModel;
        viewModel.ChatViewModel = chatViewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshPlantsAsync();
    }

    private async void OnPlantSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Plant plant)
        {
            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            await Shell.Current.GoToAsync($"PlantDetail?nickname={Uri.EscapeDataString(plant.Nickname)}");
        }
    }

    private async void OnAddPlantClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddPlant");
    }
}

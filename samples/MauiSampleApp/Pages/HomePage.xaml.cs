using MauiSampleApp.Core.Models;
using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomePageViewModel _viewModel;

    public HomePage(HomePageViewModel viewModel, ChatViewModel chatViewModel)
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

    private void OnOpenChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = true;
    }

    private async void OnCloseChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = false;
        await _viewModel.RefreshPlantsAsync();
    }

    private async void OnChatBackdropTapped(object? sender, TappedEventArgs e)
    {
        ChatOverlay.IsVisible = false;
        await _viewModel.RefreshPlantsAsync();
    }
}

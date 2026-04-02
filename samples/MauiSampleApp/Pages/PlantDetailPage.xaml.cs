using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class PlantDetailPage : ContentPage
{
    public PlantDetailPage(PlantDetailViewModel viewModel, ChatViewModel chatViewModel)
    {
        viewModel.ChatViewModel = chatViewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    private void OnActionsTabClicked(object? sender, EventArgs e)
    {
        ActionsContent.IsVisible = true;
        HistoryContent.IsVisible = false;
        ActionsTab.BackgroundColor = Color.FromArgb("#5B8C5A");
        ActionsTab.TextColor = Colors.White;
        HistoryTab.BackgroundColor = Color.FromArgb("#E8E0D4");
        HistoryTab.TextColor = Color.FromArgb("#2C2416");
    }

    private void OnHistoryTabClicked(object? sender, EventArgs e)
    {
        ActionsContent.IsVisible = false;
        HistoryContent.IsVisible = true;
        HistoryTab.BackgroundColor = Color.FromArgb("#5B8C5A");
        HistoryTab.TextColor = Colors.White;
        ActionsTab.BackgroundColor = Color.FromArgb("#E8E0D4");
        ActionsTab.TextColor = Color.FromArgb("#2C2416");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

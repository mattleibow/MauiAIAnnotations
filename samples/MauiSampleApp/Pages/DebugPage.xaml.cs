using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class DebugPage : ContentPage
{
    private readonly DebugViewModel _viewModel;

    public DebugPage(DebugViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataAsync();
    }

    private async void OnClearAllClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Clear All Data", "Are you sure you want to delete all data?", "Yes", "No");
        if (confirm)
        {
            await _viewModel.ClearAllDataAsync();
            await _viewModel.LoadDataAsync();
        }
    }
}

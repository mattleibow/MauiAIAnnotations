using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class PlantDetailPage : ContentPage
{
    public PlantDetailPage(PlantDetailViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

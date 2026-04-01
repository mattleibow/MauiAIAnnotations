using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class AddPlantPage : ContentPage
{
    public AddPlantPage(AddPlantViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}

using MauiSampleApp.Pages;

namespace MauiSampleApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("PlantDetail", typeof(PlantDetailPage));
        Routing.RegisterRoute("AddPlant", typeof(AddPlantPage));
    }
}

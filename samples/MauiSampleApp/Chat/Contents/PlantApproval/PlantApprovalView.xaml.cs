namespace MauiSampleApp.Chat;

public partial class PlantApprovalView : ContentView
{
    public PlantApprovalView(PlantApprovalViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }
}

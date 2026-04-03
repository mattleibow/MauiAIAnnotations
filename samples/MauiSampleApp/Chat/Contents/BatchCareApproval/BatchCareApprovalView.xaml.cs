namespace MauiSampleApp.Chat;

public partial class BatchCareApprovalView : ContentView
{
    public BatchCareApprovalView(BatchCareApprovalViewModel vm)
    {
        BindingContext = vm;
        InitializeComponent();
    }
}

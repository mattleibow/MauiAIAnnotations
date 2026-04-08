using MauiAIAnnotations.Maui.Chat;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalView : ContentContextView
{
    private readonly PlantApprovalViewModel _vm = new();

    public PlantApprovalViewModel ViewModel => _vm;

    public PlantApprovalView()
    {
        InitializeComponent();
    }

    protected override void RefreshFromContentContext()
    {
        if (ContentContext is not null)
            _vm.ApplyContentContext(ContentContext);
    }
}

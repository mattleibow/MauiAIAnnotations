using Microsoft.Extensions.AI.Maui.Chat;

namespace MauiSampleApp.Chat;

public partial class BatchCareApprovalView : ContentContextView
{
    private readonly BatchCareApprovalViewModel _vm = new();

    public BatchCareApprovalViewModel ViewModel => _vm;

    public BatchCareApprovalView()
    {
        InitializeComponent();
    }

    protected override void RefreshFromContentContext()
    {
        if (ContentContext is not null)
            _vm.ApplyContentContext(ContentContext);
    }
}

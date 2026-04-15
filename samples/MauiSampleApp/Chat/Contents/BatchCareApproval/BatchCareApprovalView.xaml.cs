using Microsoft.Extensions.AI.Maui.Chat;

namespace MauiSampleApp.Chat;

using Microsoft.Extensions.AI;

public partial class BatchCareApprovalView : ContentContextView, IToolApprovalResponseFactory
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

    public ToolApprovalResponseContent CreateApprovalResponse(ToolApprovalRequestContent request, bool approved) =>
        _vm.CreateApprovalResponse(request, approved);
}

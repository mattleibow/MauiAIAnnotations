using Microsoft.Extensions.AI.Maui.Chat;

namespace MauiSampleApp.Chat;

using Microsoft.Extensions.AI;

public partial class PlantApprovalView : ContentContextView, IToolApprovalResponseFactory
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

    public ToolApprovalResponseContent CreateApprovalResponse(ToolApprovalRequestContent request, bool approved) =>
        _vm.CreateApprovalResponse(request, approved);
}

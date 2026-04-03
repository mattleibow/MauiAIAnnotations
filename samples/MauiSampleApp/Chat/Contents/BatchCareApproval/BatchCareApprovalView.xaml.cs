using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Content-only approval view for batch care events.
/// Shows checkboxes for each care item. Modifies FunctionCallContent.Arguments directly.
/// </summary>
public partial class BatchCareApprovalView : ContentView
{
    private readonly BatchCareApprovalViewModel _vm = new();

    public BatchCareApprovalView()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context &&
            context.Content is ToolApprovalRequestContent approval &&
            approval.ToolCall is FunctionCallContent fc)
        {
            _vm.LoadFromArguments(fc.Arguments);
            _vm.PropertyChanged += (_, _) => _vm.WriteTo(fc);
            BindingContext = _vm;
        }
    }
}

using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Content-only approval view for add_plant.
/// Modifies FunctionCallContent.Arguments directly via the PlantApprovalViewModel.
/// The library wrapper handles header, buttons, and read-only state.
/// </summary>
public partial class PlantApprovalView : ContentView
{
    private readonly PlantApprovalViewModel _vm = new();

    public PlantApprovalView()
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
            _vm.LoadFrom(fc.Arguments);
            _vm.PropertyChanged += (_, _) => _vm.WriteTo(fc);
            BindingContext = _vm;
        }
    }
}

using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.Controls;
using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalView : ContentView
{
    private PlantApprovalViewModel? _vm;

    public PlantApprovalView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            _vm ??= new PlantApprovalViewModel();
            _vm.SetContext(context);
            this.BindingContext = _vm;
        }
    }

    private void OnApproveClicked(object? sender, EventArgs e)
    {
        if (_vm?.Request is not ToolApprovalRequestContent request)
            return;

        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, true, _vm.BuildArguments());
    }

    private void OnRejectClicked(object? sender, EventArgs e)
    {
        if (_vm?.Request is not ToolApprovalRequestContent request)
            return;

        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, false);
    }

    private ChatViewModel? FindChatViewModel()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is ChatPanelControl panel)
                return panel.ChatVM;
            if (current is ChatOverlayControl overlay)
                return overlay.ChatVM;
            current = current.Parent;
        }
        return null;
    }
}

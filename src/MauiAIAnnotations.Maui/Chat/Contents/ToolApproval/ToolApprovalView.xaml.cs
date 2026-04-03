using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class ToolApprovalView : ContentView
{
    public ToolApprovalView() => InitializeComponent();

    private void OnApproveClicked(object? sender, EventArgs e) => Respond(true);
    private void OnRejectClicked(object? sender, EventArgs e) => Respond(false);

    private void Respond(bool approved)
    {
        if (BindingContext is not ContentContext context ||
            context.Content is not ToolApprovalRequestContent request)
            return;

        // Walk up the visual tree to find the ChatViewModel
        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, approved);
    }

    /// <summary>
    /// Walks up the visual tree to find the ChatViewModel from the ChatPanelControl or ChatOverlayControl.
    /// </summary>
    private ChatViewModel? FindChatViewModel()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is Controls.ChatPanelControl panel)
                return panel.ChatVM;
            if (current is Controls.ChatOverlayControl overlay)
                return overlay.ChatVM;
            current = current.Parent;
        }
        return null;
    }
}

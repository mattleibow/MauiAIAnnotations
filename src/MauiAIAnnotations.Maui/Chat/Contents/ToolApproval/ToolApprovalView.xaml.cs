using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class ToolApprovalView : ContentView
{
    /// <summary>
    /// Optional inner content view type. When set, the wrapper resolves this view
    /// from DI (supporting constructor injection) and places it in the content slot.
    /// The view or its BindingContext can implement <see cref="IContentContextAware"/>
    /// to receive the <see cref="ContentContext"/> — like MAUI's IQueryAttributable.
    /// </summary>
    internal Type? InnerContentType { get; set; }

    public ToolApprovalView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not ContentContext context)
            return;

        if (InnerContentType is not null)
        {
            // Resolve from DI (supports constructor injection for VMs)
            var services = Handler?.MauiContext?.Services;
            var innerView = services is not null
                ? (View)(services.GetService(InnerContentType) ?? Activator.CreateInstance(InnerContentType)!)
                : (View)Activator.CreateInstance(InnerContentType)!;

            // IContentContextAware — check view then its BindingContext (like IQueryAttributable)
            var aware = innerView as IContentContextAware ?? innerView.BindingContext as IContentContextAware;
            aware?.ApplyContentContext(context);

            InnerContentSlot.Content = innerView;
        }
        else
        {
            InnerContentSlot.Content = BuildDefaultArgsView(context);
        }

        if (context.ApprovalResolved)
            InnerContentSlot.IsEnabled = false;
    }

    private static View BuildDefaultArgsView(ContentContext context)
    {
        var args = context.ApprovalArguments;
        if (args is null || args.Count == 0)
            return new Label { Text = "(no arguments)", FontSize = 12, TextColor = Colors.Gray };

        var stack = new VerticalStackLayout { Spacing = 4 };
        foreach (var kvp in args)
        {
            var row = new HorizontalStackLayout { Spacing = 6 };
            row.Add(new Label
            {
                Text = $"{kvp.Key}:", FontSize = 12,
                FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7A7062")
            });
            row.Add(new Label
            {
                Text = kvp.Value?.ToString() ?? "(null)", FontSize = 12,
                TextColor = Color.FromArgb("#2C2416")
            });
            stack.Add(row);
        }
        return stack;
    }

    private void OnApproveClicked(object? sender, EventArgs e) => Respond(true);
    private void OnRejectClicked(object? sender, EventArgs e) => Respond(false);

    private void Respond(bool approved)
    {
        if (BindingContext is not ContentContext context ||
            context.Content is not ToolApprovalRequestContent request)
            return;

        InnerContentSlot.IsEnabled = false;

        var args = approved && request.ToolCall is FunctionCallContent fc ? fc.Arguments : null;

        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, approved, args);
    }

    private ChatViewModel? FindChatViewModel()
    {
        Element? current = this;
        while (current is not null)
        {
            if (current is Controls.ChatPanelControl panel)
                return panel.ChatVM;
            current = current.Parent;
        }
        return null;
    }
}

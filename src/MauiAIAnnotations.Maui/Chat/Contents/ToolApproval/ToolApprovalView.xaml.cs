using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class ToolApprovalView : ContentView
{
    private IApprovalContentProvider? _contentProvider;

    /// <summary>
    /// Optional inner content view type. When set, the wrapper creates this view
    /// in the content slot instead of the default argument list.
    /// The inner view should implement <see cref="IApprovalContentProvider"/>.
    /// </summary>
    internal Type? InnerContentType { get; set; }

    public ToolApprovalView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not ContentContext context)
            return;

        // Build inner content
        if (InnerContentType is not null)
        {
            var innerView = (View)Activator.CreateInstance(InnerContentType)!;
            if (innerView is IApprovalContentProvider provider)
            {
                _contentProvider = provider;
                provider.Initialize(context.ApprovalArguments);
            }
            InnerContentSlot.Content = innerView;
        }
        else
        {
            // Default: show arguments as read-only key/value list
            InnerContentSlot.Content = BuildDefaultArgsView(context);
        }

        // If already resolved (recycled cell), disable
        if (context.ApprovalResolved)
            _contentProvider?.SetReadOnly(true);
    }

    private static View BuildDefaultArgsView(ContentContext context)
    {
        var args = context.ApprovalArguments;
        if (args is null || args.Count == 0)
            return new Label
            {
                Text = "(no arguments)",
                FontSize = 12,
                TextColor = Colors.Gray
            };

        var stack = new VerticalStackLayout { Spacing = 4 };
        foreach (var kvp in args)
        {
            var row = new HorizontalStackLayout { Spacing = 6 };
            row.Add(new Label
            {
                Text = $"{kvp.Key}:",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7A7062")
            });
            row.Add(new Label
            {
                Text = kvp.Value?.ToString() ?? "(null)",
                FontSize = 12,
                TextColor = Color.FromArgb("#2C2416")
            });
            stack.Add(row);
        }
        return stack;
    }

    private void OnApproveClicked(object? sender, EventArgs e)
    {
        var args = _contentProvider?.GetArguments();
        Respond(true, args);
    }

    private void OnRejectClicked(object? sender, EventArgs e) => Respond(false, null);

    private void Respond(bool approved, IDictionary<string, object?>? modifiedArgs)
    {
        if (BindingContext is not ContentContext context ||
            context.Content is not ToolApprovalRequestContent request)
            return;

        _contentProvider?.SetReadOnly(true);

        var chatVm = FindChatViewModel();
        chatVm?.RespondToApproval(request, approved, modifiedArgs);
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

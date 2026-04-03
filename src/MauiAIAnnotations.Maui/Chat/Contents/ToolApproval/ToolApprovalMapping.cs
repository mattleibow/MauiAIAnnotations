using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Matches <see cref="ToolApprovalRequestContent"/> items in the chat.
/// Optionally filters by tool name. Optionally specifies a custom inner content view.
/// </summary>
public class ToolApprovalMapping : ContentTemplateMapping
{
    /// <summary>Filter to a specific tool name, or null to match all approval requests.</summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Optional custom inner content view type. When set, the approval wrapper
    /// creates this view in its content slot instead of the default args display.
    /// The view should implement <see cref="IApprovalContentProvider"/> to
    /// support editable arguments and read-only mode after resolution.
    /// </summary>
    public Type? InnerViewType { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is ToolApprovalRequestContent approval &&
        (ToolName is null || (approval.ToolCall is FunctionCallContent fc &&
            string.Equals(fc.Name, ToolName, StringComparison.OrdinalIgnoreCase)));

    internal override DataTemplate GetTemplate()
    {
        // Always use ToolApprovalView as the wrapper; pass InnerViewType through
        var innerType = InnerViewType;
        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var wrapper = new ToolApprovalView();
            wrapper.InnerContentType = innerType;
            return wrapper;
        });
    }

    private DataTemplate? _cachedTemplate;
}

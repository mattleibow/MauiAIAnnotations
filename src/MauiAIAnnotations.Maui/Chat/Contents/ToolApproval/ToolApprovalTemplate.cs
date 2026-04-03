using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Matches <see cref="ToolApprovalRequestContent"/> items in the chat.
/// Optionally filters by tool name.
/// Set <see cref="ContentTemplate.ViewType"/> to a custom inner content view;
/// leave null for the default arguments display.
/// </summary>
public class ToolApprovalTemplate : ContentTemplate
{
    /// <summary>Filter to a specific tool name, or null to match all approval requests.</summary>
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is ToolApprovalRequestContent approval &&
        (ToolName is null || (approval.ToolCall is FunctionCallContent fc &&
            string.Equals(fc.Name, ToolName, StringComparison.OrdinalIgnoreCase)));

    internal override DataTemplate GetTemplate()
    {
        var innerType = ViewType;
        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var wrapper = new ToolApprovalView();
            wrapper.InnerContentType = innerType;
            // Explicit template lookup — implicit styles may not resolve inside CollectionView
            wrapper.SetDynamicResource(ContentView.ControlTemplateProperty, "MauiAI.ToolApprovalTemplate");
            return wrapper;
        });
    }

    private DataTemplate? _cachedTemplate;
}

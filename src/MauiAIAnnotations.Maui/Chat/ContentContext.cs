using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Wraps an <see cref="AIContent"/> item with its role for display in a chat UI.
/// Views subscribe to PropertyChanged on Content to react to streaming updates.
/// </summary>
public partial class ContentContext : ObservableObject
{
    [NotifyPropertyChangedFor(nameof(ToolName))]
    [ObservableProperty]
    public partial AIContent Content { get; set; }

    public ContentRole Role { get; }

    /// <summary>
    /// Gets the associated tool name when this content represents a tool call,
    /// approval request, or tool result.
    /// </summary>
    public string? ToolName => ToolNameOverride ?? Content switch
    {
        FunctionCallContent call => call.Name,
        ToolApprovalRequestContent approval when approval.ToolCall is FunctionCallContent call => call.Name,
        _ => null,
    };

    internal string? ToolNameOverride { get; init; }

    internal Action<ToolApprovalRequestContent, bool>? ApprovalResponder { get; init; }

    /// <summary>Whether the approval has been resolved (approved or rejected).</summary>
    [ObservableProperty]
    public partial bool ApprovalResolved { get; set; }

    /// <summary>Status text shown after resolution (e.g. "✅ Approved" or "❌ Rejected").</summary>
    [ObservableProperty]
    public partial string? ApprovalResolutionText { get; set; }

    public ContentContext(AIContent content, ContentRole role)
    {
        Content = content;
        Role = role;
    }

    public ContentContext(AIContent content, string role)
        : this(content, ParseRole(role))
    {
    }

    private static ContentRole ParseRole(string role) =>
        Enum.TryParse<ContentRole>(role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : throw new ArgumentOutOfRangeException(nameof(role), role,
                $"Unknown content role '{role}'.");
}

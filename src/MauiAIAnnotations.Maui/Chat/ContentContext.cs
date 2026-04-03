using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Wraps an <see cref="AIContent"/> item with its role for display in a chat UI.
/// Views subscribe to PropertyChanged on Content to react to streaming updates.
/// </summary>
public partial class ContentContext : ObservableObject
{
    [ObservableProperty]
    public partial AIContent Content { get; set; }

    public string Role { get; }

    /// <summary>Whether the approval has been resolved (approved or rejected).</summary>
    [ObservableProperty]
    public partial bool ApprovalResolved { get; set; }

    /// <summary>Status text shown after resolution (e.g. "✅ Approved" or "❌ Rejected").</summary>
    [ObservableProperty]
    public partial string? ApprovalResolutionText { get; set; }

    public ContentContext(AIContent content, string role)
    {
        Content = content;
        Role = role;
    }
}

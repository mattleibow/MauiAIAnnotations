using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Headless chat session contract that can be hosted by MAUI, WinForms, console apps, or other UI layers.
/// </summary>
public interface IChatSession
{
    event EventHandler<ChatSessionChangedEventArgs>? Changed;

    IReadOnlyList<ChatEntry> Messages { get; }

    IReadOnlyCollection<ChatEntry> PendingApprovals { get; }

    bool IsBusy { get; }

    bool HasPendingApprovals { get; }

    bool AllowMultipleToolCalls { get; set; }

    string? ConversationId { get; }

    string? SystemPrompt { get; set; }

    Task SendAsync(string userMessage, CancellationToken cancellationToken = default);

    Task SubmitApprovalAsync(ToolApprovalResponseContent response, CancellationToken cancellationToken = default);

    void Clear();
}

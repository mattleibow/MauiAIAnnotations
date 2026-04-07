using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Coordinates approval-required tool calls between the chat pipeline and the app UI.
/// </summary>
public interface IToolApprovalCoordinator
{
    /// <summary>
    /// Gets a value indicating whether there are approval requests currently waiting on a user decision.
    /// </summary>
    bool HasPendingApprovals { get; }

    /// <summary>
    /// Raised whenever <see cref="HasPendingApprovals"/> changes.
    /// </summary>
    event EventHandler? PendingApprovalsChanged;

    /// <summary>
    /// Waits until all supplied approval requests have been answered.
    /// </summary>
    /// <param name="requests">The pending approval requests.</param>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>The approval responses in the same order as the incoming requests.</returns>
    ValueTask<IReadOnlyList<ToolApprovalResponseContent>> WaitForApprovalAsync(
        IReadOnlyList<ToolApprovalRequestContent> requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits until all supplied approval requests have been answered for the specified session or conversation scope.
    /// </summary>
    /// <param name="scopeId">The chat/session scope that owns the requests.</param>
    /// <param name="requests">The pending approval requests.</param>
    /// <param name="cancellationToken">A token used to cancel the wait.</param>
    /// <returns>The approval responses in the same order as the incoming requests.</returns>
    ValueTask<IReadOnlyList<ToolApprovalResponseContent>> WaitForApprovalAsync(
        string scopeId,
        IReadOnlyList<ToolApprovalRequestContent> requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to submit an approval response for the currently pending request batch.
    /// </summary>
    /// <param name="response">The approval response to submit.</param>
    /// <returns><see langword="true"/> if the response was accepted; otherwise, <see langword="false"/>.</returns>
    bool TrySubmit(ToolApprovalResponseContent response);

    /// <summary>
    /// Attempts to submit an approval response for the currently pending request batch within a specific session scope.
    /// </summary>
    /// <param name="scopeId">The chat/session scope that owns the approval request.</param>
    /// <param name="response">The approval response to submit.</param>
    /// <returns><see langword="true"/> if the response was accepted; otherwise, <see langword="false"/>.</returns>
    bool TrySubmit(string scopeId, ToolApprovalResponseContent response);

    /// <summary>
    /// Cancels any approval requests that are currently waiting.
    /// </summary>
    void CancelPending();

    /// <summary>
    /// Cancels any approval requests that are currently waiting within the specified session scope.
    /// </summary>
    /// <param name="scopeId">The chat/session scope to cancel.</param>
    void CancelPending(string scopeId);
}

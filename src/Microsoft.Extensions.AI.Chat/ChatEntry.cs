using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI.Chat;

/// <summary>
/// Immutable transcript item emitted by a <see cref="ChatSession"/>.
/// </summary>
public sealed record ChatEntry(
    string Id,
    AIContent Content,
    ContentRole Role,
    string? ToolName = null,
    ToolApprovalState ApprovalState = ToolApprovalState.None);

using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Immutable transcript item emitted by a <see cref="ChatSession"/>.
/// </summary>
public sealed record ChatEntry(
    string Id,
    AIContent Content,
    ContentRole Role,
    string? ToolName = null,
    ToolApprovalState ApprovalState = ToolApprovalState.None);

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// A chat-client middleware that pauses on <see cref="ToolApprovalRequestContent"/> and resumes once the
/// app has provided matching <see cref="ToolApprovalResponseContent"/> items.
/// </summary>
public sealed class ToolApprovalChatClient : DelegatingChatClient
{
    private const string ApprovalScopeKey = "mauiapproval_scope_id";
    private readonly IToolApprovalCoordinator _approvalCoordinator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolApprovalChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The wrapped chat client.</param>
    /// <param name="approvalCoordinator">The coordinator used to wait for and submit approval responses.</param>
    public ToolApprovalChatClient(IChatClient innerClient, IToolApprovalCoordinator approvalCoordinator)
        : base(innerClient)
    {
        _approvalCoordinator = approvalCoordinator ?? throw new ArgumentNullException(nameof(approvalCoordinator));
    }

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var history = messages.ToList();
        var scopeId = ResolveScopeId(history, options);

        while (true)
        {
            var iterationUpdates = new List<ChatResponseUpdate>();
            var approvalRequests = new List<ToolApprovalRequestContent>();

            await foreach (var update in InnerClient.GetStreamingResponseAsync(history, options, cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                var capturedUpdate = update.Clone();
                iterationUpdates.Add(capturedUpdate);

                foreach (var content in capturedUpdate.Contents)
                {
                    if (content is ToolApprovalRequestContent request)
                    {
                        approvalRequests.Add(request);
                    }
                }

                yield return capturedUpdate;
            }

            if (approvalRequests.Count == 0)
            {
                yield break;
            }

            history.AddMessages(iterationUpdates);

            var responses = await _approvalCoordinator.WaitForApprovalAsync(scopeId, approvalRequests, cancellationToken).ConfigureAwait(false);
            if (responses.Count == 0)
            {
                yield break;
            }

            var approvalResponseUpdate = new ChatResponseUpdate(ChatRole.User, [.. responses]);
            yield return approvalResponseUpdate;
            history.AddMessages(approvalResponseUpdate);
        }
    }

    private static string ResolveScopeId(IReadOnlyList<ChatMessage> history, ChatOptions? options)
    {
        if (!string.IsNullOrWhiteSpace(options?.ConversationId))
            return options.ConversationId!;

        if (options?.AdditionalProperties?.TryGetValue(ApprovalScopeKey, out var rawScopeId) == true &&
            rawScopeId is string optionScopeId &&
            !string.IsNullOrWhiteSpace(optionScopeId))
        {
            return optionScopeId;
        }

        foreach (var message in history.Reverse())
        {
            foreach (var content in message.Contents)
            {
                if (content.AdditionalProperties?.TryGetValue(ApprovalScopeKey, out rawScopeId) == true &&
                    rawScopeId is string contentScopeId &&
                    !string.IsNullOrWhiteSpace(contentScopeId))
                {
                    return contentScopeId;
                }
            }
        }

        return ToolApprovalCoordinator.DefaultScopeId;
    }
}

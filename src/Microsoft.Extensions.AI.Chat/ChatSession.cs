using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI.Chat;

/// <summary>
/// Headless orchestration engine for a single conversation.
/// </summary>
public sealed class ChatSession : IChatSession, IDisposable
{
    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private readonly List<ChatMessage> _conversationHistory = [];
    private readonly List<ChatEntry> _messages = [];
    private readonly Dictionary<string, string> _toolNamesByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatEntry> _pendingApprovalsById = new(StringComparer.Ordinal);
    private CancellationTokenSource? _activeRequestCancellation;

    public ChatSession(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools?.ToList() ?? throw new ArgumentNullException(nameof(tools));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    public event EventHandler<ChatSessionChangedEventArgs>? Changed;

    public IReadOnlyList<ChatEntry> Messages => _messages;

    public IReadOnlyCollection<ChatEntry> PendingApprovals => _pendingApprovalsById.Values.ToList().AsReadOnly();

    public bool IsBusy { get; private set; }

    public bool HasPendingApprovals => _pendingApprovalsById.Count > 0;

    public bool AllowMultipleToolCalls { get; set; }

    public string? ConversationId { get; private set; }

    public string? SystemPrompt { get; set; }

    public void Clear()
    {
        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
        _activeRequestCancellation = null;

        _conversationHistory.Clear();
        _toolNamesByCallId.Clear();
        _pendingApprovalsById.Clear();
        _messages.Clear();
        ConversationId = null;
        SetIsBusy(false);
        OnChanged(ChatSessionChangeKind.Reset);
    }

    public async Task SendAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return;

        var trimmedMessage = userMessage.Trim();
        AddEntry(new TextContent(trimmedMessage), ContentRole.User);

        await ContinueConversationAsync(new ChatMessage(ChatRole.User, trimmedMessage), cancellationToken);
    }

    public async Task SubmitApprovalAsync(ToolApprovalResponseContent response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_pendingApprovalsById.TryGetValue(response.RequestId, out var pendingEntry))
            throw new InvalidOperationException("This approval request is no longer pending.");

        if (pendingEntry.Content is not ToolApprovalRequestContent request)
            throw new InvalidOperationException("Pending approval entries must be backed by a ToolApprovalRequestContent.");

        if (!IsValidApprovalResponse(request, response))
        {
            throw new InvalidOperationException(
                "Edited approval responses must preserve the original tool call identity.");
        }

        ReplaceEntry(
            pendingEntry,
            pendingEntry with
            {
                ApprovalState = response.Approved ? ToolApprovalState.Approved : ToolApprovalState.Rejected,
            });

        _pendingApprovalsById.Remove(response.RequestId);
        OnChanged(ChatSessionChangeKind.StateChanged);

        await ContinueConversationAsync(new ChatMessage(ChatRole.User, [response]), cancellationToken);
    }

    public void Dispose()
    {
        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
    }

    private async Task ContinueConversationAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        SetIsBusy(true);

        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();

        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeRequestCancellation = requestCancellation;
        _conversationHistory.Add(message);

        try
        {
            await RunStreamingLoopAsync(requestCancellation.Token);
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            AddEntry(new ErrorContent(ex.Message), ContentRole.Error);
        }
        finally
        {
            requestCancellation.Dispose();

            if (ReferenceEquals(_activeRequestCancellation, requestCancellation))
                _activeRequestCancellation = null;

            SetIsBusy(false);
        }
    }

    private async Task RunStreamingLoopAsync(CancellationToken cancellationToken)
    {
        var options = new ChatOptions
        {
            Tools = [.. _tools],
            AllowMultipleToolCalls = AllowMultipleToolCalls,
            ConversationId = ConversationId,
        };

        var history = BuildHistory();
        ChatEntry? assistantEntry = null;
        var responseText = string.Empty;
        var responseUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            responseUpdates.Add(update.Clone());

            if (!string.IsNullOrWhiteSpace(update.ConversationId) &&
                !string.Equals(ConversationId, update.ConversationId, StringComparison.Ordinal))
            {
                ConversationId = update.ConversationId;
                OnChanged(ChatSessionChangeKind.StateChanged);
            }

            foreach (var content in update.Contents)
                ProcessResponseContent(content, ref assistantEntry, ref responseText);
        }

        if (responseUpdates.Count > 0)
        {
            _conversationHistory.AddMessages(responseUpdates);
            return;
        }

        if (assistantEntry is null && responseText.Length == 0)
        {
            var noResponse = new TextContent("(no response)");
            AddEntry(noResponse, ContentRole.Assistant);
            _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, [noResponse]));
        }
    }

    private void ProcessResponseContent(
        AIContent content,
        ref ChatEntry? assistantEntry,
        ref string responseText)
    {
        switch (content)
        {
            case ToolApprovalRequestContent approval:
            {
                var approvalEntry = AddEntry(approval, ContentRole.Approval, ToolApprovalState.Pending);
                _pendingApprovalsById[approval.RequestId] = approvalEntry;
                OnChanged(ChatSessionChangeKind.StateChanged);
                break;
            }

            case ToolApprovalResponseContent response:
                if (_pendingApprovalsById.TryGetValue(response.RequestId, out var pendingEntry))
                {
                    ReplaceEntry(
                        pendingEntry,
                        pendingEntry with
                        {
                            ApprovalState = response.Approved ? ToolApprovalState.Approved : ToolApprovalState.Rejected,
                        });
                    _pendingApprovalsById.Remove(response.RequestId);
                    OnChanged(ChatSessionChangeKind.StateChanged);
                }
                break;

            case FunctionCallContent call:
                AddEntry(call, ContentRole.Tool);
                break;

            case FunctionResultContent result:
                AddEntry(result, ContentRole.Tool);
                break;

            case TextContent textContent when textContent.Text is not null:
                responseText += textContent.Text;
                if (assistantEntry is null)
                {
                    assistantEntry = AddEntry(new TextContent(responseText), ContentRole.Assistant);
                }
                else
                {
                    assistantEntry = ReplaceEntry(assistantEntry, assistantEntry with
                    {
                        Content = new TextContent(responseText),
                    });
                }
                break;
        }
    }

    private List<ChatMessage> BuildHistory()
    {
        var history = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
            history.Add(new ChatMessage(ChatRole.System, SystemPrompt));

        history.AddRange(_conversationHistory);
        return history;
    }

    private ChatEntry AddEntry(AIContent content, ContentRole role, ToolApprovalState approvalState = ToolApprovalState.None)
    {
        var entry = new ChatEntry(
            Guid.NewGuid().ToString("n"),
            content,
            role,
            ResolveToolName(content),
            approvalState);

        _messages.Add(entry);
        OnChanged(ChatSessionChangeKind.MessageAdded, entry, _messages.Count - 1);
        return entry;
    }

    private ChatEntry ReplaceEntry(ChatEntry previous, ChatEntry updated)
    {
        var index = _messages.FindIndex(message => message.Id == previous.Id);
        if (index < 0)
            return updated;

        _messages[index] = updated;
        OnChanged(ChatSessionChangeKind.MessageUpdated, updated, index);
        return updated;
    }

    private string? ResolveToolName(AIContent content)
    {
        switch (content)
        {
            case FunctionCallContent call:
                if (!string.IsNullOrWhiteSpace(call.CallId))
                    _toolNamesByCallId[call.CallId] = call.Name;
                return call.Name;

            case ToolApprovalRequestContent approval when approval.ToolCall is FunctionCallContent call:
                if (!string.IsNullOrWhiteSpace(call.CallId))
                    _toolNamesByCallId[call.CallId] = call.Name;
                return call.Name;

            case FunctionResultContent result when
                !string.IsNullOrWhiteSpace(result.CallId) &&
                _toolNamesByCallId.TryGetValue(result.CallId, out var toolName):
                return toolName;

            default:
                return null;
        }
    }

    private void SetIsBusy(bool isBusy)
    {
        if (IsBusy == isBusy)
            return;

        IsBusy = isBusy;
        OnChanged(ChatSessionChangeKind.StateChanged);
    }

    private static bool IsValidApprovalResponse(
        ToolApprovalRequestContent request,
        ToolApprovalResponseContent response)
    {
        if (response.ToolCall is null)
            return true;

        if (request.ToolCall is FunctionCallContent originalCall &&
            response.ToolCall is FunctionCallContent editedCall)
        {
            return string.Equals(editedCall.CallId, originalCall.CallId, StringComparison.Ordinal) &&
                   string.Equals(editedCall.Name, originalCall.Name, StringComparison.OrdinalIgnoreCase);
        }

        return request.ToolCall?.GetType() == response.ToolCall.GetType();
    }

    private void OnChanged(ChatSessionChangeKind kind, ChatEntry? entry = null, int? index = null) =>
        Changed?.Invoke(this, new ChatSessionChangedEventArgs(kind, entry, index));
}

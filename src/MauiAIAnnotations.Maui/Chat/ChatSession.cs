using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using Microsoft.Maui.ApplicationModel;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Session-scoped chat state and orchestration for a single conversation.
/// </summary>
public partial class ChatSession : ObservableObject
{
    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private readonly List<ChatMessage> _conversationHistory = [];
    private readonly Dictionary<string, string> _toolNamesByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContentContext> _pendingApprovalsById = new(StringComparer.Ordinal);
    private readonly ObservableCollection<ContentContext> _pendingApprovals = [];
    private CancellationTokenSource? _activeRequestCancellation;

    [ObservableProperty]
    public partial string UserInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasPendingApprovals { get; set; }

    public string SystemPrompt { get; set; } = """
        You are a friendly assistant. You help users with their tasks and answer questions.
        You have access to tools to perform various operations.
        Be conversational and helpful.
        """;

    /// <summary>
    /// Gets or sets a value indicating whether the underlying chat client may propose multiple tool calls in one turn.
    /// </summary>
    public bool AllowMultipleToolCalls { get; set; }

    /// <summary>
    /// Gets the conversation/thread id reported by the underlying chat client when one is available.
    /// </summary>
    public string? ConversationId { get; private set; }

    public ObservableCollection<ContentContext> Messages { get; } = [];

    public ReadOnlyObservableCollection<ContentContext> PendingApprovals { get; }

    public ChatSession(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools?.ToList() ?? throw new ArgumentNullException(nameof(tools));
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        PendingApprovals = new ReadOnlyObservableCollection<ContentContext>(_pendingApprovals);
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear()
    {
        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
        _activeRequestCancellation = null;

        _conversationHistory.Clear();
        _toolNamesByCallId.Clear();
        _pendingApprovalsById.Clear();
        _pendingApprovals.Clear();

        ConversationId = null;
        HasPendingApprovals = false;
        UserInput = string.Empty;
        Messages.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput.Trim();
        UserInput = string.Empty;

        await InvokeOnMainThreadAsync(() =>
        {
            Messages.Add(CreateContext(new TextContent(userMessage), ContentRole.User));
        }).ConfigureAwait(false);

        await ContinueConversationAsync(new ChatMessage(ChatRole.User, userMessage), CancellationToken.None)
            .ConfigureAwait(false);
    }

    public Task RespondToApprovalAsync(ToolApprovalResponseContent response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_pendingApprovalsById.TryGetValue(response.RequestId, out var context))
        {
            throw new InvalidOperationException("This approval request is no longer pending.");
        }

        return SubmitApprovalAsync(context, response);
    }

    private async Task SubmitApprovalAsync(ContentContext context, ToolApprovalResponseContent response)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(response);

        _pendingApprovalsById.Remove(response.RequestId, out var pendingContext);
        pendingContext ??= context;

        _pendingApprovals.Remove(pendingContext);
        HasPendingApprovals = _pendingApprovals.Count > 0;

        var toolName = pendingContext.ToolName ?? "Tool";
        await InvokeOnMainThreadAsync(() =>
        {
            pendingContext.ApprovalResolved = true;
            pendingContext.ApprovalResolutionText = response.Approved
                ? $"Approved - {toolName}"
                : $"Rejected - {toolName}";
        }).ConfigureAwait(false);

        await ContinueConversationAsync(new ChatMessage(ChatRole.User, [response]), CancellationToken.None)
            .ConfigureAwait(false);
    }

    private async Task ContinueConversationAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        await InvokeOnMainThreadAsync(() => IsBusy = true).ConfigureAwait(false);

        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();

        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeRequestCancellation = requestCancellation;
        _conversationHistory.Add(message);

        try
        {
            // Keep the network/chat pipeline off the UI thread on Android, but marshal UI updates
            // back through InvokeOnMainThreadAsync when messages and approval state change.
            await Task.Run(() => RunStreamingLoopAsync(requestCancellation.Token), requestCancellation.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(CreateContext(new ErrorContent(ex.Message), ContentRole.Error));
            }).ConfigureAwait(false);
        }
        finally
        {
            requestCancellation.Dispose();

            if (ReferenceEquals(_activeRequestCancellation, requestCancellation))
                _activeRequestCancellation = null;

            await InvokeOnMainThreadAsync(() => IsBusy = false).ConfigureAwait(false);
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
        ContentContext? assistantCtx = null;
        var responseText = string.Empty;
        var responseUpdates = new List<ChatResponseUpdate>();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            responseUpdates.Add(update.Clone());

            if (!string.IsNullOrWhiteSpace(update.ConversationId))
            {
                var conversationId = update.ConversationId;
                await InvokeOnMainThreadAsync(() => ConversationId = conversationId).ConfigureAwait(false);
            }

            foreach (var content in update.Contents)
            {
                await InvokeOnMainThreadAsync(() =>
                {
                    ProcessResponseContent(content, ref assistantCtx, ref responseText);
                }).ConfigureAwait(false);
            }
        }

        if (responseUpdates.Count > 0)
        {
            _conversationHistory.AddMessages(responseUpdates);
            return;
        }

        if (assistantCtx is null && responseText.Length == 0)
        {
            var noResponse = new TextContent("(no response)");
            await InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(CreateContext(noResponse, ContentRole.Assistant));
            }).ConfigureAwait(false);
            _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, [noResponse]));
        }
    }

    private void ProcessResponseContent(
        AIContent content,
        ref ContentContext? assistantCtx,
        ref string responseText)
    {
        switch (content)
        {
            case ToolApprovalRequestContent approval:
                var approvalContext = CreateContext(approval, ContentRole.Approval);
                RegisterPendingApproval(approval, approvalContext);
                Messages.Add(approvalContext);
                break;

            case ToolApprovalResponseContent response:
                MarkApprovalResolved(response);
                break;

            case FunctionCallContent call:
                Messages.Add(CreateContext(call, ContentRole.Tool));
                break;

            case FunctionResultContent result:
                Messages.Add(CreateContext(result, ContentRole.Tool));
                break;

            case TextContent textContent when textContent.Text is not null:
                responseText += textContent.Text;
                if (assistantCtx is null)
                {
                    assistantCtx = CreateContext(new TextContent(responseText), ContentRole.Assistant);
                    Messages.Add(assistantCtx);
                }
                else
                {
                    assistantCtx.Content = new TextContent(responseText);
                }
                break;
        }
    }

    private void RegisterPendingApproval(ToolApprovalRequestContent request, ContentContext context)
    {
        _pendingApprovalsById[request.RequestId] = context;

        if (!_pendingApprovals.Contains(context))
            _pendingApprovals.Add(context);

        HasPendingApprovals = _pendingApprovals.Count > 0;
    }

    private void MarkApprovalResolved(ToolApprovalResponseContent response)
    {
        if (!_pendingApprovalsById.Remove(response.RequestId, out var context))
        {
            HasPendingApprovals = _pendingApprovals.Count > 0;
            return;
        }

        _pendingApprovals.Remove(context);
        HasPendingApprovals = _pendingApprovals.Count > 0;

        if (!context.ApprovalResolved)
        {
            var toolName = context.ToolName ?? "Tool";
            context.ApprovalResolved = true;
            context.ApprovalResolutionText = response.Approved
                ? $"Approved - {toolName}"
                : $"Rejected - {toolName}";
        }
    }

    private List<ChatMessage> BuildHistory()
    {
        var history = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(SystemPrompt))
            history.Add(new ChatMessage(ChatRole.System, SystemPrompt));

        history.AddRange(_conversationHistory);
        return history;
    }

    private ContentContext CreateContext(AIContent content, ContentRole role)
    {
        var toolName = TrackToolName(content);
        ContentContext? context = null;

        context = new ContentContext(content, role)
        {
            ToolNameOverride = toolName,
            ApprovalResponder = content is ToolApprovalRequestContent
                ? RespondToApprovalAsync
                : null,
        };

        return context;
    }

    private string? TrackToolName(AIContent content)
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

    private static Task InvokeOnMainThreadAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            if (MainThread.IsMainThread)
            {
                action();
                return Task.CompletedTask;
            }

            return MainThread.InvokeOnMainThreadAsync(action);
        }
        catch (NotSupportedException)
        {
            action();
            return Task.CompletedTask;
        }
    }
}

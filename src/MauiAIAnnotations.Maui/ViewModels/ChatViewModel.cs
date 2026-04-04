using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private readonly List<ChatMessage> _conversationHistory = [];
    private readonly Dictionary<string, string> _toolNamesByCallId = new(StringComparer.Ordinal);

    private TaskCompletionSource<List<ToolApprovalResponseContent>>? _approvalTcs;
    private List<ToolApprovalRequestContent> _pendingApprovals = [];
    private readonly Dictionary<ToolApprovalRequestContent, ToolApprovalResponseContent> _approvalResponses = [];
    private CancellationTokenSource? _activeRequestCancellation;

    [ObservableProperty]
    public partial string UserInput { get; set; }

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

    public ObservableCollection<ContentContext> Messages { get; } = [];

    public ChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear()
    {
        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
        _activeRequestCancellation = null;

        _approvalTcs?.TrySetCanceled();
        _approvalTcs = null;
        _pendingApprovals.Clear();
        _approvalResponses.Clear();
        _conversationHistory.Clear();
        _toolNamesByCallId.Clear();

        HasPendingApprovals = false;
        Messages.Clear();
    }

    /// <summary>
    /// Respond to a pending tool approval request. Call this from approval UI views.
    /// </summary>
    /// <param name="request">The approval request to respond to.</param>
    /// <param name="approved">Whether the user approved the tool call.</param>
    /// <param name="modifiedArguments">Optional modified arguments (user can edit before approving).</param>
    public void RespondToApproval(ToolApprovalRequestContent request, bool approved, IDictionary<string, object?>? modifiedArguments = null)
    {
        if (_approvalTcs is null || !_pendingApprovals.Contains(request))
            return;

        if (_approvalResponses.ContainsKey(request))
            return;

        ToolApprovalResponseContent response;

        if (approved && modifiedArguments is not null && request.ToolCall is FunctionCallContent originalCall)
        {
            var modifiedCall = new FunctionCallContent(originalCall.CallId, originalCall.Name, modifiedArguments);
            response = new ToolApprovalResponseContent(request.RequestId, true, modifiedCall);
        }
        else
        {
            response = request.CreateResponse(approved, approved ? null : "User rejected");
        }

        // Mark the approval card as resolved in-place (don't replace — preserve chat history)
        var toolName = request.ToolCall is FunctionCallContent fc ? fc.Name : "Tool";
        var approvalCtx = Messages.FirstOrDefault(m => ReferenceEquals(m.Content, request));
        if (approvalCtx is not null)
        {
            approvalCtx.ApprovalResolved = true;
            approvalCtx.ApprovalResolutionText = approved
                ? $"✅ Approved — {toolName}"
                : $"❌ Rejected — {toolName}";
        }

        _approvalResponses[request] = response;
        HasPendingApprovals = _pendingApprovals.Any(p => !_approvalResponses.ContainsKey(p));

        if (_approvalTcs is not null && _pendingApprovals.All(_approvalResponses.ContainsKey))
        {
            var responses = _pendingApprovals.Select(p => _approvalResponses[p]).ToList();
            _approvalTcs.TrySetResult(responses);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput;
        UserInput = string.Empty;
        Messages.Add(CreateContext(new TextContent(userMessage), ContentRole.User));
        _conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));
        IsBusy = true;

        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
        using var requestCancellation = new CancellationTokenSource();
        _activeRequestCancellation = requestCancellation;

        try
        {
            await RunStreamingLoopAsync(requestCancellation.Token);
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Messages.Add(CreateContext(new ErrorContent(ex.Message), ContentRole.Error));
        }
        finally
        {
            IsBusy = false;
            if (ReferenceEquals(_activeRequestCancellation, requestCancellation))
                _activeRequestCancellation = null;
        }
    }

    private async Task RunStreamingLoopAsync(CancellationToken cancellationToken)
    {
        var options = new ChatOptions { Tools = [.. _tools] };

        while (true)
        {
            var history = BuildHistory();
            ContentContext? assistantCtx = null;
            var responseText = "";
            var assistantContents = new List<AIContent>();
            var toolContents = new List<AIContent>();
            var approvalRequests = new List<ToolApprovalRequestContent>();

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options, cancellationToken))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent approval:
                            approvalRequests.Add(approval);
                            assistantContents.Add(approval);
                            Messages.Add(CreateContext(approval, ContentRole.Approval));
                            break;

                        case FunctionCallContent call:
                            assistantContents.Add(call);
                            Messages.Add(CreateContext(call, ContentRole.Tool));
                            break;

                        case FunctionResultContent result:
                            toolContents.Add(result);
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
            }

            if (responseText.Length > 0)
                assistantContents.Add(new TextContent(responseText));

            AddToConversationHistory(ChatRole.Assistant, assistantContents);
            AddToConversationHistory(ChatRole.Tool, toolContents);

            // If no approvals needed, we're done
            if (approvalRequests.Count == 0)
            {
                if (assistantCtx is null && responseText.Length == 0 && assistantContents.Count == 0 && toolContents.Count == 0)
                {
                    var noResponse = new TextContent("(no response)");
                    Messages.Add(CreateContext(noResponse, ContentRole.Assistant));
                    AddToConversationHistory(ChatRole.Assistant, [noResponse]);
                }

                return;
            }

            // Wait for user to respond to approvals
            _pendingApprovals = approvalRequests;
            HasPendingApprovals = true;
            _approvalResponses.Clear();
            _approvalTcs = new TaskCompletionSource<List<ToolApprovalResponseContent>>();
            var responses = await _approvalTcs.Task.WaitAsync(cancellationToken);
            _approvalTcs = null;
            _pendingApprovals.Clear();
            _approvalResponses.Clear();
            HasPendingApprovals = false;

            AddToConversationHistory(ChatRole.User, responses);

            // Check if all were rejected
            if (responses.All(r => !r.Approved))
            {
                var rejection = new TextContent("Tool call was rejected.");
                Messages.Add(CreateContext(rejection, ContentRole.Assistant));
                AddToConversationHistory(ChatRole.Assistant, [rejection]);
                return;
            }
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
        return new ContentContext(content, role)
        {
            ToolNameOverride = toolName,
            ApprovalResponder = content is ToolApprovalRequestContent ? RespondToApproval : null,
        };
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

    private void AddToConversationHistory(ChatRole role, IEnumerable<AIContent> contents)
    {
        var materializedContents = contents.ToList();
        if (materializedContents.Count == 0)
            return;

        _conversationHistory.Add(new ChatMessage(role, [.. materializedContents]));
    }
}

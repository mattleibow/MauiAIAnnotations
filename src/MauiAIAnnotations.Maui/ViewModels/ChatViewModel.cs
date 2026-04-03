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

    private TaskCompletionSource<List<ToolApprovalResponseContent>>? _approvalTcs;
    private List<ToolApprovalRequestContent> _pendingApprovals = [];

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

    /// <summary>
    /// Additional tools to include per-request (e.g., VM-specific ad-hoc tools).
    /// Override in subclass or set before sending.
    /// </summary>
    public IList<AITool> AdditionalTools { get; } = new List<AITool>();

    public ChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear() => Messages.Clear();

    /// <summary>
    /// Respond to a pending tool approval request. Call this from approval UI views.
    /// </summary>
    /// <param name="request">The approval request to respond to.</param>
    /// <param name="approved">Whether the user approved the tool call.</param>
    /// <param name="modifiedArguments">Optional modified arguments (user can edit before approving).</param>
    public void RespondToApproval(ToolApprovalRequestContent request, bool approved, IDictionary<string, object?>? modifiedArguments = null)
    {
        ToolApprovalResponseContent response;
        if (approved && modifiedArguments is not null && request.ToolCall is FunctionCallContent originalCall)
        {
            // Create a new FunctionCallContent with modified arguments
            var modifiedCall = new FunctionCallContent(originalCall.CallId, originalCall.Name, modifiedArguments);
            response = new ToolApprovalResponseContent(request.RequestId, true, modifiedCall);
        }
        else
        {
            response = request.CreateResponse(approved, approved ? null : "User rejected");
        }

        // Collect the response
        if (_approvalTcs is not null)
        {
            // Find and complete the pending approval batch
            var responses = new List<ToolApprovalResponseContent>();
            foreach (var pending in _pendingApprovals)
            {
                if (pending == request)
                    responses.Add(response);
                else
                    responses.Add(pending.CreateResponse(true)); // Auto-approve others in batch
            }
            _pendingApprovals.Clear();
            HasPendingApprovals = false;
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
        Messages.Add(new ContentContext(new TextContent(userMessage), "User"));
        IsBusy = true;

        try
        {
            await RunStreamingLoopAsync();
        }
        catch (Exception ex)
        {
            Messages.Add(new ContentContext(new ErrorContent(ex.Message), "Error"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunStreamingLoopAsync()
    {
        var history = BuildHistory();
        var options = new ChatOptions { Tools = [.. AdditionalTools, .. _tools] };

        while (true)
        {
            ContentContext? assistantCtx = null;
            var responseText = "";
            var approvalRequests = new List<ToolApprovalRequestContent>();

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case ToolApprovalRequestContent approval:
                            approvalRequests.Add(approval);
                            Messages.Add(new ContentContext(approval, "Approval"));
                            break;

                        case FunctionCallContent call:
                            Messages.Add(new ContentContext(call, "Tool"));
                            break;

                        case FunctionResultContent result:
                            Messages.Add(new ContentContext(result, "Tool"));
                            break;

                        case TextContent textContent when textContent.Text is not null:
                            responseText += textContent.Text;
                            if (assistantCtx is null)
                            {
                                assistantCtx = new ContentContext(new TextContent(responseText), "Assistant");
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

            // If no approvals needed, we're done
            if (approvalRequests.Count == 0)
            {
                if (assistantCtx is null && responseText.Length == 0)
                    Messages.Add(new ContentContext(new TextContent("(no response)"), "Assistant"));
                return;
            }

            // Wait for user to respond to approvals
            _pendingApprovals = approvalRequests;
            HasPendingApprovals = true;
            _approvalTcs = new TaskCompletionSource<List<ToolApprovalResponseContent>>();
            var responses = await _approvalTcs.Task;
            _approvalTcs = null;

            // Check if all were rejected
            if (responses.All(r => !r.Approved))
            {
                Messages.Add(new ContentContext(new TextContent("Tool call was rejected."), "Assistant"));
                return;
            }

            // Append the approval exchange to history so the next iteration has full context
            history.Add(new ChatMessage(ChatRole.Assistant, [.. approvalRequests]));
            history.Add(new ChatMessage(ChatRole.User, [.. responses]));
        }
    }

    private List<ChatMessage> BuildHistory()
    {
        var history = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(SystemPrompt))
            history.Add(new ChatMessage(ChatRole.System, SystemPrompt));

        foreach (var m in Messages)
        {
            if (m.Content is TextContent text)
            {
                var role = m.Role == "User" ? ChatRole.User : ChatRole.Assistant;
                history.Add(new ChatMessage(role, text.Text ?? ""));
            }
        }

        return history;
    }
}

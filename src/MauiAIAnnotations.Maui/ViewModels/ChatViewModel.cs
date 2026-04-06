using System.Collections.ObjectModel;
using MauiAIAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly IChatClient _chatClient;
    private readonly IToolApprovalCoordinator _toolApprovalCoordinator;
    private readonly IList<AITool> _tools;
    private readonly List<ChatMessage> _conversationHistory = [];
    private readonly Dictionary<string, string> _toolNamesByCallId = new(StringComparer.Ordinal);
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

    public ChatViewModel(
        IEnumerable<AITool> tools,
        IChatClient chatClient,
        IToolApprovalCoordinator toolApprovalCoordinator)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
        _toolApprovalCoordinator = toolApprovalCoordinator;
        HasPendingApprovals = _toolApprovalCoordinator.HasPendingApprovals;
        _toolApprovalCoordinator.PendingApprovalsChanged += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                HasPendingApprovals = _toolApprovalCoordinator.HasPendingApprovals;
            });
        };
    }

    private bool CanSend() => !IsBusy;

    [RelayCommand]
    private void Clear()
    {
        _activeRequestCancellation?.Cancel();
        _activeRequestCancellation?.Dispose();
        _activeRequestCancellation = null;

        _toolApprovalCoordinator.CancelPending();
        _conversationHistory.Clear();
        _toolNamesByCallId.Clear();

        HasPendingApprovals = false;
        Messages.Clear();
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
            await Task.Run(() => RunStreamingLoopAsync(requestCancellation.Token), requestCancellation.Token);
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
        var options = new ChatOptions
        {
            Tools = [.. _tools],
            // MEAI notes that when one call in a response needs approval, all tool calls in
            // that same response enter the approval flow. Prefer one call at a time.
            AllowMultipleToolCalls = false,
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

            foreach (var content in update.Contents)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ProcessResponseContent(content, ref assistantCtx, ref responseText);
                });
            }
        }

        if (responseUpdates.Count > 0)
        {
            _conversationHistory.AddMessages(responseUpdates);
            await MainThread.InvokeOnMainThreadAsync(() => HasPendingApprovals = false);
            return;
        }

        if (assistantCtx is null && responseText.Length == 0)
        {
            var noResponse = new TextContent("(no response)");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Add(CreateContext(noResponse, ContentRole.Assistant));
            });
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
                HasPendingApprovals = true;
                Messages.Add(CreateContext(approval, ContentRole.Approval));
                break;

            case ToolApprovalResponseContent:
                HasPendingApprovals = _toolApprovalCoordinator.HasPendingApprovals;
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
            ApprovalResponder = content is ToolApprovalRequestContent ? _toolApprovalCoordinator.TrySubmit : null,
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
}

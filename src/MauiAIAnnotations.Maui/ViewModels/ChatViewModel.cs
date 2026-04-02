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

    [ObservableProperty]
    public partial string UserInput { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial bool IsBusy { get; set; }

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

            var options = new ChatOptions { Tools = [.. AdditionalTools, .. _tools] };

            ContentContext? assistantCtx = null;
            var responseText = "";

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
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

            if (assistantCtx is null)
                Messages.Add(new ContentContext(new TextContent("(no response)"), "Assistant"));
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
}

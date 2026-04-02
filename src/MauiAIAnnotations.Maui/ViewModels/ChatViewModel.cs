using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private static readonly string SystemPrompt = """
        You are a friendly assistant. You help users with their tasks and answer questions.
        You have access to tools to perform various operations.
        Be conversational and helpful.
        """;

    public ChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
    }

    /// <summary>
    /// Additional tools to include per-request (e.g., VM-specific ad-hoc tools).
    /// Override in subclass or set before sending.
    /// </summary>
    public IList<AITool> AdditionalTools { get; } = new List<AITool>();

    public ObservableCollection<ContentContext> Messages { get; }

    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public ICommand SendCommand { get; }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput;
        UserInput = string.Empty;

        Messages.Add(new ContentContext(new TextContent(userMessage), "User"));
        IsBusy = true;

        try
        {
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

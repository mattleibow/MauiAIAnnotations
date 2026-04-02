using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient _chatClient;
    private readonly IList<AITool> _tools;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private static readonly string SystemPrompt = """
        You are a friendly gardening assistant. You help users manage their plants, track care events, and answer plant care questions.
        You have access to tools to look up species information, manage plants, and log care events.
        When a user mentions adding a plant, use the AddPlant tool. When they ask about care, look up the species profile and care history.
        Be conversational and helpful. Use emoji occasionally to be friendly 🌱
        """;

    public ChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
    }

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
            // Build history from messages
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };
            foreach (var m in Messages)
            {
                if (m.Content is TextContent text)
                {
                    var role = m.Role == "User" ? ChatRole.User : ChatRole.Assistant;
                    history.Add(new ChatMessage(role, text.Text ?? ""));
                }
            }

            var options = new ChatOptions { Tools = [GetChatContextTool(), .. _tools] };

            // Streaming response — accumulate text into a single ContentContext
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
                                // Update in-place for streaming effect
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

    private AITool GetChatContextTool() =>
        AIFunctionFactory.Create(
            () => new
            {
                MessageCount = Messages.Count,
                UserMessages = Messages.Count(m => m.Role == "User"),
                AssistantMessages = Messages.Count(m => m.Role == "Assistant"),
                LastUserMessage = Messages.Where(m => m.Role == "User" && m.Content is TextContent)
                    .Select(m => ((TextContent)m.Content).Text)
                    .LastOrDefault(),
            },
            "get_chat_context",
            "Gets context about the current conversation including message count and last user message.");
}

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

    public ObservableCollection<ChatEntry> Messages { get; }

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
        Messages.Add(new ChatEntry { Type = ChatEntryType.UserText, Content = userMessage });
        IsBusy = true;

        try
        {
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };

            foreach (var m in Messages)
            {
                var role = m.Type == ChatEntryType.UserText ? ChatRole.User : ChatRole.Assistant;
                history.Add(new ChatMessage(role, m.Content));
            }

            // Combine DI-registered tools with VM-specific ad-hoc tools.
            // This demonstrates the spread pattern for adding per-request
            // tools that have access to ViewModel state.
            var options = new ChatOptions { Tools = [GetChatContextTool(), .. _tools] };

            // Streaming response — use a mutable ChatEntry so the UI updates in-place
            var assistantEntry = new ChatEntry { Type = ChatEntryType.AssistantText, Content = "" };
            Messages.Add(assistantEntry);
            var responseText = "";

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                if (update.Text is { } text)
                {
                    responseText += text;
                    assistantEntry.Content = responseText;
                }
            }

            if (string.IsNullOrEmpty(responseText))
                assistantEntry.Content = "(no response)";
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatEntry { Type = ChatEntryType.Error, Content = ex.Message });
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Creates a VM-specific ad-hoc tool that gives the AI context about the
    /// current conversation. This tool accesses ViewModel state (Messages)
    /// and is created fresh each send — demonstrating per-request tools that
    /// can't be registered in DI because they depend on runtime state.
    /// </summary>
    private AITool GetChatContextTool() =>
        AIFunctionFactory.Create(
            () => new
            {
                MessageCount = Messages.Count,
                UserMessages = Messages.Count(m => m.Type == ChatEntryType.UserText),
                AssistantMessages = Messages.Count(m => m.Type == ChatEntryType.AssistantText),
                LastUserMessage = Messages.LastOrDefault(m => m.Type == ChatEntryType.UserText)?.Content,
            },
            "get_chat_context",
            "Gets context about the current conversation including message count and last user message. " +
            "Use this to understand the conversation history when the user refers to earlier messages.");
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiAIAnnotations;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.ViewModels;

public record ConversationEntry(string Role, string Content);

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient _chatClient;
    private readonly IAIToolProvider _toolProvider;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private static readonly string SystemPrompt = """
        You are a friendly gardening assistant. You help users manage their plants, track care events, and answer plant care questions.
        You have access to tools to look up species information, manage plants, and log care events.
        When a user mentions adding a plant, use the AddPlant tool. When they ask about care, look up the species profile and care history.
        Be conversational and helpful. Use emoji occasionally to be friendly 🌱
        """;

    public ChatViewModel(IAIToolProvider toolProvider, IChatClient chatClient)
    {
        _toolProvider = toolProvider;
        _chatClient = chatClient;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
    }

    public ObservableCollection<ConversationEntry> Messages { get; }

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
        Messages.Add(new ConversationEntry("User", userMessage));
        IsBusy = true;

        try
        {
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };

            foreach (var m in Messages)
            {
                history.Add(m.Role == "User"
                    ? new ChatMessage(ChatRole.User, m.Content)
                    : new ChatMessage(ChatRole.Assistant, m.Content));
            }

            var options = new ChatOptions { Tools = _toolProvider.GetTools().ToList() };

            // Streaming response
            var index = Messages.Count;
            Messages.Add(new ConversationEntry("Assistant", ""));
            var responseText = "";

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                if (update.Text is { } text)
                {
                    responseText += text;
                    Messages[index] = new ConversationEntry("Assistant", responseText);
                }
            }

            if (string.IsNullOrEmpty(responseText))
                Messages[index] = new ConversationEntry("Assistant", "(no response)");
        }
        catch (Exception ex)
        {
            Messages.Add(new ConversationEntry("Assistant", $"Error: {ex.Message}"));
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

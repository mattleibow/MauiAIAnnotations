using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace AnnotationsSampleApp;

public partial class MainPage : ContentPage
{
    private readonly IServiceProvider _rootProvider;
    private readonly IChatClient _innerChatClient;
    private IServiceScope? _sessionScope;
    private IChatClient? _sessionClient;
    private List<ChatMessage> _history = [];
    private string _currentToolMode = "All Tools";
    private bool _isBusy;

    public MainPage(IServiceProvider rootProvider, IChatClient innerChatClient)
    {
        _rootProvider = rootProvider;
        _innerChatClient = innerChatClient;
        InitializeComponent();
        StartNewSession();
    }

    private void StartNewSession()
    {
        _sessionScope?.Dispose();
        _sessionScope = _rootProvider.CreateScope();
        _history =
        [
            new(ChatRole.System,
                "You are a helpful gardening assistant. Help users browse plants, manage their garden, and get seasonal advice. Be concise and friendly.")
        ];

        // Build the tool list based on current mode
        var tools = GetToolsForMode(_currentToolMode);

        // Build a chat client pipeline with function invocation for this session's tools
        _sessionClient = new ChatClientBuilder(_innerChatClient)
            .UseFunctionInvocation()
            .Build(_sessionScope.ServiceProvider);

        // Clear the UI
        MessagesStack.Children.Clear();
        AddSystemMessage($"🌱 New chat session started — {_currentToolMode} ({tools.Count} tools)");
    }

    private IReadOnlyList<AITool> GetToolsForMode(string mode) => mode switch
    {
        "Catalog Only" => CatalogTools.Default.GetTools(_sessionScope!.ServiceProvider),
        "Garden Management" => GardenManagementTools.Default.GetTools(_sessionScope!.ServiceProvider),
        _ => AllGardenTools.Default.GetTools(_sessionScope!.ServiceProvider),
    };

    private void OnNewChatClicked(object? sender, EventArgs e)
    {
        StartNewSession();
    }

    private void OnToolModeChanged(object? sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedItem is not string selected)
            return;

        if (selected == _currentToolMode)
            return;

        _currentToolMode = selected;
        StartNewSession();
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = ChatInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || _isBusy || _sessionClient is null)
            return;

        ChatInput.Text = string.Empty;
        SetBusy(true);

        // Show user message
        AddUserMessage(text);
        _history.Add(new ChatMessage(ChatRole.User, text));

        try
        {
            var tools = GetToolsForMode(_currentToolMode);
            var options = new ChatOptions { Tools = [.. tools] };

            var responseText = string.Empty;
            Label? responseLabel = null;

            await foreach (var update in _sessionClient.GetStreamingResponseAsync(_history, options))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent call:
                            AddToolMessage($"🔧 Calling: {call.Name}");
                            break;

                        case FunctionResultContent result:
                            var resultText = result.Result?.ToString() ?? "(no result)";
                            if (resultText.Length > 200)
                                resultText = resultText[..200] + "...";
                            AddToolMessage($"✅ Result: {resultText}");
                            break;

                        case TextContent tc when tc.Text is not null:
                            responseText += tc.Text;
                            if (responseLabel is null)
                            {
                                responseLabel = AddAssistantMessage(responseText);
                            }
                            else
                            {
                                responseLabel.Text = responseText;
                            }
                            break;
                    }
                }
            }

            // Add the full response to history
            if (!string.IsNullOrEmpty(responseText))
                _history.Add(new ChatMessage(ChatRole.Assistant, responseText));

            if (responseLabel is null && string.IsNullOrEmpty(responseText))
                AddAssistantMessage("(no response)");
        }
        catch (Exception ex)
        {
            AddErrorMessage(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        ChatInput.IsEnabled = !busy;
    }

    private void AddUserMessage(string text)
    {
        var frame = new Border
        {
            BackgroundColor = Color.FromArgb("#DCF8C6"),
            Padding = new Thickness(12, 8),
            HorizontalOptions = LayoutOptions.End,
            MaximumWidthRequest = 300,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Content = new Label
            {
                Text = text,
                FontSize = 14,
                TextColor = Colors.Black,
            }
        };
        MessagesStack.Children.Add(frame);
        ScrollToBottom();
    }

    private Label AddAssistantMessage(string text)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 14,
        };
        var frame = new Border
        {
            BackgroundColor = Color.FromArgb("#F0F0F0"),
            Padding = new Thickness(12, 8),
            HorizontalOptions = LayoutOptions.Start,
            MaximumWidthRequest = 300,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            StrokeThickness = 0,
            Content = label,
        };
        MessagesStack.Children.Add(frame);
        ScrollToBottom();
        return label;
    }

    private void AddToolMessage(string text)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 12,
            TextColor = Colors.Gray,
            FontAttributes = FontAttributes.Italic,
            Padding = new Thickness(8, 2),
        };
        MessagesStack.Children.Add(label);
        ScrollToBottom();
    }

    private void AddSystemMessage(string text)
    {
        var label = new Label
        {
            Text = text,
            FontSize = 12,
            TextColor = Color.FromArgb("#5B8C5A"),
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            Padding = new Thickness(8, 4),
        };
        MessagesStack.Children.Add(label);
        ScrollToBottom();
    }

    private void AddErrorMessage(string text)
    {
        var label = new Label
        {
            Text = $"❌ Error: {text}",
            FontSize = 12,
            TextColor = Colors.Red,
            Padding = new Thickness(8, 2),
        };
        MessagesStack.Children.Add(label);
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), async () =>
        {
            await ChatScrollView.ScrollToAsync(0, ChatScrollView.ContentSize.Height, true);
        });
    }
}

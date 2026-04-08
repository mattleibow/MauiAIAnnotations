using MauiAIAnnotations;

namespace MauiSampleApp.Pages;

public partial class ChatPage : ContentPage
{
    public ChatSession ChatSession { get; }

    public ChatPage(ChatSession chatSession)
    {
        ChatSession = chatSession;
        InitializeComponent();
    }

    private void OnClearChatClicked(object? sender, EventArgs e)
    {
        ChatSession.Clear();
    }
}

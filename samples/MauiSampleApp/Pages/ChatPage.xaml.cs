using MauiAIAnnotations.Maui.Chat;

namespace MauiSampleApp.Pages;

public partial class ChatPage : ContentPage
{
    public ChatSession ChatSession { get; }

    public ChatPage(ChatSession chatSession)
    {
        ChatSession = chatSession;
        InitializeComponent();
    }
}

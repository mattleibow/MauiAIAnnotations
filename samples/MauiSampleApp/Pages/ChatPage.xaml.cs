using MauiAIAnnotations.Maui.ViewModels;

namespace MauiSampleApp.Pages;

public partial class ChatPage : ContentPage
{
    public ChatViewModel ChatViewModel { get; }

    public ChatPage(ChatViewModel chatViewModel)
    {
        ChatViewModel = chatViewModel;
        InitializeComponent();
    }
}

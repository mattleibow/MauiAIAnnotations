using MauiAIAnnotations.Maui.ViewModels;

namespace MauiSampleApp.Pages;

public partial class ChatPage : ContentPage
{
    public ChatPage(ChatViewModel chatViewModel)
    {
        BindingContext = chatViewModel;
        InitializeComponent();
    }
}

using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class ChatPage : ContentPage
{
    public ChatPage(GardenChatViewModel chatViewModel)
    {
        BindingContext = chatViewModel;
        InitializeComponent();
    }
}

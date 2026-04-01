namespace MauiSampleApp;

public partial class ChatPage : ContentPage
{
    public ChatPage(ViewModels.ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

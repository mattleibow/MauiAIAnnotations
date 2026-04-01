using MauiSampleApp.ViewModels;

namespace MauiSampleApp;

public partial class MainPage : ContentPage
{
    private readonly ChatPage _chatPage;

    public MainPage(MainPageViewModel viewModel, ChatPage chatPage)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _chatPage = chatPage;
    }

    private async void OnOpenChatClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(_chatPage);
    }
}

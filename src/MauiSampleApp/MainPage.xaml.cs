using MauiSampleApp.ViewModels;

namespace MauiSampleApp;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel, ChatViewModel chatViewModel)
    {
        viewModel.ChatViewModel = chatViewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    private void OnOpenChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = true;
    }

    private void OnCloseChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = false;
    }

    private void OnChatBackdropTapped(object? sender, TappedEventArgs e)
    {
        ChatOverlay.IsVisible = false;
    }
}

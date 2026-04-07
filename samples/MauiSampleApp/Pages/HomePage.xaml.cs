using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Core.Models;
using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Pages;

public partial class HomePage : ContentPage
{
    private const double CollapsedTrayHeight = 84;
    private const string ChatTrayAnimationName = "HomeChatTrayHeight";
    private readonly HomePageViewModel _viewModel;
    private bool _isChatTrayOpen;
    private double _panStartHeight;

    public ChatSession ChatSession { get; }

    public HomePage(HomePageViewModel viewModel, ChatSession chatSession)
    {
        _viewModel = viewModel;
        ChatSession = chatSession;
        BindingContext = viewModel;
        InitializeComponent();
        Loaded += OnPageLoaded;
        SizeChanged += OnPageSizeChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.RefreshPlantsAsync();
    }

    private async void OnPlantSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Plant plant)
        {
            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            await Shell.Current.GoToAsync($"PlantDetail?nickname={Uri.EscapeDataString(plant.Nickname)}");
        }
    }

    private async void OnAddPlantClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddPlant");
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        ApplyChatTrayState(animated: false);
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (Height <= 0)
            return;

        HomeChatTray.MaximumHeightRequest = GetExpandedTrayHeight();
        if (_isChatTrayOpen)
            HomeChatTray.HeightRequest = GetExpandedTrayHeight();
    }

    private async void OnChatTrayHeaderTapped(object? sender, TappedEventArgs e)
    {
        await SetChatTrayOpenAsync(!_isChatTrayOpen);
    }

    private async void OnChatTrayToggleClicked(object? sender, EventArgs e)
    {
        await SetChatTrayOpenAsync(!_isChatTrayOpen);
    }

    private async void OnChatTrayScrimTapped(object? sender, TappedEventArgs e)
    {
        await SetChatTrayOpenAsync(false);
    }

    private async void OnChatTrayPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                this.AbortAnimation(ChatTrayAnimationName);
                _panStartHeight = HomeChatTray.Height > 0
                    ? HomeChatTray.Height
                    : (_isChatTrayOpen ? GetExpandedTrayHeight() : CollapsedTrayHeight);
                break;

            case GestureStatus.Running:
                var nextHeight = Math.Clamp(_panStartHeight - e.TotalY, CollapsedTrayHeight, GetExpandedTrayHeight());
                ApplyTrayProgress(nextHeight);
                break;

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                var midpoint = (CollapsedTrayHeight + GetExpandedTrayHeight()) / 2;
                await SetChatTrayOpenAsync(HomeChatTray.HeightRequest >= midpoint);
                break;
        }
    }

    private double GetExpandedTrayHeight()
    {
        if (Height <= 0)
            return 520;

        var maxHeight = Math.Max(CollapsedTrayHeight, Height - 16);
        return Math.Min(maxHeight, Height * 0.9);
    }

    private void ApplyChatTrayState(bool animated)
    {
        _ = SetChatTrayOpenAsync(_isChatTrayOpen, animated);
    }

    private void ApplyTrayProgress(double currentHeight)
    {
        HomeChatTray.HeightRequest = currentHeight;

        var expandedHeight = GetExpandedTrayHeight();
        var progress = expandedHeight <= CollapsedTrayHeight
            ? 0
            : (currentHeight - CollapsedTrayHeight) / (expandedHeight - CollapsedTrayHeight);

        ChatTrayBody.IsVisible = progress > 0.05;
        ChatTrayScrim.IsVisible = progress > 0.01;
        ChatTrayScrim.InputTransparent = progress <= 0.01;
        ChatTrayScrim.Opacity = Math.Clamp(progress * 0.35, 0, 0.35);
        UpdateTrayHeader(progress > 0.5);
    }

    private async Task SetChatTrayOpenAsync(bool isOpen, bool animate = true)
    {
        _isChatTrayOpen = isOpen;

        var targetHeight = isOpen ? GetExpandedTrayHeight() : CollapsedTrayHeight;
        var startingHeight = HomeChatTray.HeightRequest > 0 ? HomeChatTray.HeightRequest : CollapsedTrayHeight;

        UpdateTrayHeader(isOpen);

        if (isOpen)
        {
            ChatTrayBody.IsVisible = true;
            ChatTrayScrim.IsVisible = true;
            ChatTrayScrim.InputTransparent = false;
        }

        if (!animate)
        {
            HomeChatTray.HeightRequest = targetHeight;
            ChatTrayScrim.Opacity = isOpen ? 0.35 : 0;
        }
        else
        {
            var animation = new Animation(
                callback: value => HomeChatTray.HeightRequest = value,
                start: startingHeight,
                end: targetHeight,
                easing: Easing.CubicOut);

            animation.Commit(this, ChatTrayAnimationName, length: 220);
            await ChatTrayScrim.FadeToAsync(isOpen ? 0.35 : 0, 220, Easing.CubicOut);
        }

        if (!isOpen)
        {
            ChatTrayBody.IsVisible = false;
            ChatTrayScrim.IsVisible = false;
            ChatTrayScrim.InputTransparent = true;
        }
    }

    private void UpdateTrayHeader(bool isOpen)
    {
        ChatTrayTitleLabel.Text = isOpen ? "Garden AI assistant" : "Garden AI chat";
        ChatTraySubtitleLabel.Text = isOpen
            ? "Ask about care, seasons, reminders, and garden tasks."
            : "Pull up to ask about care, seasons, and tasks.";
        ChatTrayToggleButton.Text = isOpen ? "Close" : "Open";
    }
}

using System.Collections.Specialized;
using MauiSampleApp.ViewModels;

namespace MauiSampleApp.Controls;

public partial class ChatOverlayControl : ContentView
{
    public static readonly BindableProperty ChatVMProperty =
        BindableProperty.Create(
            nameof(ChatVM),
            typeof(ChatViewModel),
            typeof(ChatOverlayControl),
            propertyChanged: OnChatVMChanged);

    public ChatViewModel? ChatVM
    {
        get => (ChatViewModel?)GetValue(ChatVMProperty);
        set => SetValue(ChatVMProperty, value);
    }

    /// <summary>
    /// Event raised when the chat overlay is closed (e.g. via close button or backdrop tap).
    /// Pages can handle this to refresh data after AI-driven changes.
    /// </summary>
    public event EventHandler? ChatClosed;

    public ChatOverlayControl()
    {
        InitializeComponent();
    }

    private static void OnChatVMChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ChatOverlayControl)bindable;

        if (oldValue is ChatViewModel oldVm)
            oldVm.Messages.CollectionChanged -= control.OnMessagesCollectionChanged;

        if (newValue is ChatViewModel newVm)
            newVm.Messages.CollectionChanged += control.OnMessagesCollectionChanged;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ChatVM is not null && ChatVM.Messages.Count > 0)
        {
            ChatMessages.ScrollTo(ChatVM.Messages.Count - 1, position: ScrollToPosition.End, animate: false);
        }
    }

    private void OnOpenChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = true;
    }

    private void OnCloseChatClicked(object? sender, EventArgs e)
    {
        ChatOverlay.IsVisible = false;
        ChatClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnChatBackdropTapped(object? sender, TappedEventArgs e)
    {
        ChatOverlay.IsVisible = false;
        ChatClosed?.Invoke(this, EventArgs.Empty);
    }
}

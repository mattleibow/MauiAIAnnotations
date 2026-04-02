using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.ViewModels;

namespace MauiAIAnnotations.Maui.Controls;

public partial class ChatOverlayControl : ContentView
{
    public static readonly BindableProperty ChatVMProperty =
        BindableProperty.Create(
            nameof(ChatVM),
            typeof(ChatViewModel),
            typeof(ChatOverlayControl));

    public ChatViewModel? ChatVM
    {
        get => (ChatViewModel?)GetValue(ChatVMProperty);
        set => SetValue(ChatVMProperty, value);
    }

    private readonly ObservableCollection<ContentTemplateMapping> _contentTemplates = [];

    public IList<ContentTemplateMapping> ContentTemplates => _contentTemplates;

    /// <summary>
    /// Event raised when the chat overlay is closed (e.g. via close button or backdrop tap).
    /// Pages can handle this to refresh data after AI-driven changes.
    /// </summary>
    public event EventHandler? ChatClosed;

    public ChatOverlayControl()
    {
        InitializeComponent();
        _contentTemplates.CollectionChanged += OnContentTemplatesChanged;
    }

    private void OnContentTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Pass templates through to the inner ChatPanelControl
        InnerChatPanel.ContentTemplates.Clear();
        foreach (var t in _contentTemplates)
            InnerChatPanel.ContentTemplates.Add(t);
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

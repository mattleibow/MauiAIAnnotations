using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.ViewModels;

namespace MauiAIAnnotations.Maui.Controls;

public partial class ChatOverlayControl : ContentView
{
    private const double SidebarWidthThreshold = 900;

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

    private bool _isChatOpen;
    private bool _isWideMode;

    public ChatOverlayControl()
    {
        InitializeComponent();
        _contentTemplates.CollectionChanged += OnContentTemplatesChanged;
        SizeChanged += OnSizeChanged;
    }

    private void OnContentTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncTemplatesTo(InnerChatPanel);
        SyncTemplatesTo(SidebarChatPanel);
    }

    private void SyncTemplatesTo(ChatPanelControl panel)
    {
        panel.ContentTemplates.Clear();
        foreach (var t in _contentTemplates)
            panel.ContentTemplates.Add(t);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        var isWide = Width >= SidebarWidthThreshold;
        if (isWide == _isWideMode)
            return;

        _isWideMode = isWide;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (_isWideMode)
        {
            // Sidebar mode: hide FAB + overlay, show sidebar if chat is open
            ChatFab.IsVisible = !_isChatOpen;
            ChatOverlay.IsVisible = false;
            SidebarPanel.IsVisible = _isChatOpen;
        }
        else
        {
            // Overlay mode: show FAB when chat is closed, overlay when open
            ChatFab.IsVisible = !_isChatOpen;
            ChatOverlay.IsVisible = _isChatOpen;
            SidebarPanel.IsVisible = false;
        }
    }

    private void OnOpenChatClicked(object? sender, EventArgs e)
    {
        _isChatOpen = true;
        UpdateLayout();
    }

    private void OnCloseChatClicked(object? sender, EventArgs e)
    {
        _isChatOpen = false;
        UpdateLayout();
        ChatClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnChatBackdropTapped(object? sender, TappedEventArgs e)
    {
        _isChatOpen = false;
        UpdateLayout();
        ChatClosed?.Invoke(this, EventArgs.Empty);
    }
}

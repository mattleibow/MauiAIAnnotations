using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.ViewModels;

namespace MauiAIAnnotations.Maui.Controls;

/// <summary>
/// A permanent chat sidebar that is always visible.
/// Unlike <see cref="ChatOverlayControl"/>, this has no FAB button,
/// no overlay, and no show/hide logic — it's just a chat panel with a header.
/// Use it in a split layout (e.g. Grid with two columns).
/// </summary>
public partial class ChatSidebarControl : ContentView
{
    public static readonly BindableProperty ChatVMProperty =
        BindableProperty.Create(
            nameof(ChatVM),
            typeof(ChatViewModel),
            typeof(ChatSidebarControl));

    public ChatViewModel? ChatVM
    {
        get => (ChatViewModel?)GetValue(ChatVMProperty);
        set => SetValue(ChatVMProperty, value);
    }

    private readonly ObservableCollection<ContentTemplateMapping> _contentTemplates = [];

    public IList<ContentTemplateMapping> ContentTemplates => _contentTemplates;

    public ChatSidebarControl()
    {
        InitializeComponent();
        _contentTemplates.CollectionChanged += OnContentTemplatesChanged;
    }

    private void OnContentTemplatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InnerChatPanel.ContentTemplates.Clear();
        foreach (var t in _contentTemplates)
            InnerChatPanel.ContentTemplates.Add(t);
    }
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.ViewModels;

namespace MauiAIAnnotations.Maui.Controls;

public partial class ChatPanelControl : ContentView
{
    public static readonly BindableProperty ChatVMProperty =
        BindableProperty.Create(
            nameof(ChatVM),
            typeof(ChatViewModel),
            typeof(ChatPanelControl),
            propertyChanged: OnChatVMChanged);

    public ChatViewModel? ChatVM
    {
        get => (ChatViewModel?)GetValue(ChatVMProperty);
        set => SetValue(ChatVMProperty, value);
    }

    private readonly ObservableCollection<ContentTemplate> _contentTemplates = [];

    public IList<ContentTemplate> ContentTemplates => _contentTemplates;

    public ChatPanelControl()
    {
        InitializeComponent();
        _contentTemplates.CollectionChanged += (_, _) => RebuildTemplateSelector();
    }

    private void RebuildTemplateSelector()
    {
        var selector = new ContentTemplateSelector();
        foreach (var t in _contentTemplates)
            selector.Templates.Add(t);
        ChatMessages.ItemTemplate = selector;
    }

    private static void OnChatVMChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ChatPanelControl)bindable;

        if (oldValue is ChatViewModel oldVm)
            oldVm.Messages.CollectionChanged -= control.OnMessagesCollectionChanged;

        if (newValue is ChatViewModel newVm)
            newVm.Messages.CollectionChanged += control.OnMessagesCollectionChanged;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ChatVM is not null && ChatVM.Messages.Count > 0)
        {
            // Dispatch with a short delay so the layout pass completes before scrolling
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
            {
                if (ChatVM is not null && ChatVM.Messages.Count > 0)
                {
                    ChatMessages.ScrollTo(ChatVM.Messages.Count - 1, position: ScrollToPosition.End, animate: true);
                }
            });
        }
    }
}

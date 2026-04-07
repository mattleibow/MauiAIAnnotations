using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using MauiAIAnnotations.Maui.Chat;

namespace MauiAIAnnotations.Maui.Controls;

public partial class ChatPanelControl : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable<ContentContext>),
            typeof(ChatPanelControl),
            propertyChanged: OnItemsSourceChanged);

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(ChatPanelControl),
            default(string),
            BindingMode.TwoWay);

    public static readonly BindableProperty IsBusyProperty =
        BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(ChatPanelControl),
            false);

    public static readonly BindableProperty SendCommandProperty =
        BindableProperty.Create(
            nameof(SendCommand),
            typeof(ICommand),
            typeof(ChatPanelControl));

    public IEnumerable<ContentContext>? ItemsSource
    {
        get => (IEnumerable<ContentContext>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public ICommand? SendCommand
    {
        get => (ICommand?)GetValue(SendCommandProperty);
        set => SetValue(SendCommandProperty, value);
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

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ChatPanelControl)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= control.OnMessagesCollectionChanged;

        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += control.OnMessagesCollectionChanged;

        control.ScrollToLatestMessage();
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (GetMessageCount() == 0)
            return;

        foreach (var delayMs in new[] { 50, 150, 300 })
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(delayMs), ScrollToLatestMessage);
    }

    private void ScrollToLatestMessage()
    {
        var messageCount = GetMessageCount();
        if (messageCount == 0)
            return;

        ChatMessages.ScrollTo(messageCount - 1, position: ScrollToPosition.End, animate: false);
    }

    private int GetMessageCount() =>
        ItemsSource switch
        {
            ICollection<ContentContext> genericCollection => genericCollection.Count,
            IReadOnlyCollection<ContentContext> readOnlyCollection => readOnlyCollection.Count,
            ICollection collection => collection.Count,
            IEnumerable<ContentContext> enumerable => enumerable.Count(),
            _ => 0,
        };
}

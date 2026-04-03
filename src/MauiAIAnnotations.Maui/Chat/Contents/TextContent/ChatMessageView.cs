using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Unified text message view for both User and Assistant roles.
/// Uses VisualStateManager to switch styling based on <see cref="MessageRole"/>.
/// </summary>
public class ChatMessageView : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(ChatMessageView));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty MessageRoleProperty =
        BindableProperty.Create(nameof(MessageRole), typeof(string), typeof(ChatMessageView));

    public string? MessageRole
    {
        get => (string?)GetValue(MessageRoleProperty);
        set => SetValue(MessageRoleProperty, value);
    }

    private ContentContext? _ctx;

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_ctx is not null)
            _ctx.PropertyChanged -= OnContentContextChanged;
        _ctx = BindingContext as ContentContext;
        if (_ctx is not null)
        {
            _ctx.PropertyChanged += OnContentContextChanged;
            Refresh();
        }
    }

    private void OnContentContextChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContentContext.Content))
            Refresh();
    }

    private void Refresh()
    {
        if (_ctx is null)
            return;
        Text = (_ctx.Content as TextContent)?.Text;
        MessageRole = _ctx.Role;
        if (Handler is not null)
            VisualStateManager.GoToState(this, _ctx.Role);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (_ctx is not null && Handler is not null)
            VisualStateManager.GoToState(this, _ctx.Role);
    }
}

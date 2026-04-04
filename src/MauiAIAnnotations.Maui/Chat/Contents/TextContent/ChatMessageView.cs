using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Unified text message view for both User and Assistant roles.
/// Uses VisualStateManager to switch styling based on <see cref="MessageRole"/>.
/// Custom templates can include a root named <c>PART_Root</c>; if omitted,
/// the view falls back to applying visual states to itself.
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
    private VisualElement? _stateRoot;

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

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _stateRoot = GetTemplateChild("PART_Root") as VisualElement;
        ApplyRoleState();
    }

    private void Refresh()
    {
        if (_ctx is null)
            return;
        Text = (_ctx.Content as TextContent)?.Text;
        MessageRole = _ctx.Role.ToString();
        ApplyRoleState();
    }

    private void ApplyRoleState()
    {
        if (_ctx is null)
            return;

        VisualStateManager.GoToState(_stateRoot ?? this, _ctx.Role.ToString());
    }
}

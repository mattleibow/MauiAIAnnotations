using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Displays an error message from an <see cref="ErrorContent"/> item.
/// Styled via a ControlTemplate that uses <c>{TemplateBinding ErrorMessageText}</c>.
/// </summary>
public class ErrorMessageView : ContentView
{
    public static readonly BindableProperty ErrorMessageTextProperty =
        BindableProperty.Create(nameof(ErrorMessageText), typeof(string), typeof(ErrorMessageView));

    public string? ErrorMessageText
    {
        get => (string?)GetValue(ErrorMessageTextProperty);
        set => SetValue(ErrorMessageTextProperty, value);
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
        ErrorMessageText = (_ctx?.Content as ErrorContent)?.Message;
    }
}

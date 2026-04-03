namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Catch-all view for content types that have no specific template.
/// Displays <c>Content.ToString()</c> via <c>{TemplateBinding DisplayText}</c>.
/// </summary>
public class DefaultMessageView : ContentView
{
    public static readonly BindableProperty DisplayTextProperty =
        BindableProperty.Create(nameof(DisplayText), typeof(string), typeof(DefaultMessageView));

    public string? DisplayText
    {
        get => (string?)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
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
        DisplayText = _ctx?.Content?.ToString();
    }
}

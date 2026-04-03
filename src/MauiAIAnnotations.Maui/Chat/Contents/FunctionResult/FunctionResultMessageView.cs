using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Fallback view for function results that don't have a custom template.
/// Displays the result as plain text via <c>{TemplateBinding ResultText}</c>.
/// </summary>
public class FunctionResultMessageView : ContentView
{
    public static readonly BindableProperty ResultTextProperty =
        BindableProperty.Create(nameof(ResultText), typeof(string), typeof(FunctionResultMessageView));

    public string? ResultText
    {
        get => (string?)GetValue(ResultTextProperty);
        set => SetValue(ResultTextProperty, value);
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
        ResultText = (_ctx?.Content as FunctionResultContent)?.Result?.ToString();
    }
}

using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Displays a function call indicator (e.g. "Calling search_plants...").
/// Styled via a ControlTemplate that uses <c>{TemplateBinding FunctionName}</c>.
/// </summary>
public class FunctionCallMessageView : ContentView
{
    public static readonly BindableProperty FunctionNameProperty =
        BindableProperty.Create(nameof(FunctionName), typeof(string), typeof(FunctionCallMessageView));

    public string? FunctionName
    {
        get => (string?)GetValue(FunctionNameProperty);
        set => SetValue(FunctionNameProperty, value);
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
        FunctionName = (_ctx?.Content as FunctionCallContent)?.Name;
    }
}

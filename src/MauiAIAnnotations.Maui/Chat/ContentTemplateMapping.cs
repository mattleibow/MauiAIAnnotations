namespace MauiAIAnnotations.Maui.Chat;

public abstract class ContentTemplateMapping : BindableObject
{
    public static readonly BindableProperty ViewTypeProperty =
        BindableProperty.Create(nameof(ViewType), typeof(Type), typeof(ContentTemplateMapping));

    public Type? ViewType
    {
        get => (Type?)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    public abstract bool When(ContentContext context);

    internal DataTemplate CreateTemplate()
    {
        var type = ViewType ?? throw new InvalidOperationException($"{GetType().Name} has no ViewType set.");
        return new DataTemplate(type);
    }
}

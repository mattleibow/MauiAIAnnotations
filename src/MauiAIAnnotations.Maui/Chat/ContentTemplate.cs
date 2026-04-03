namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Maps a <see cref="ContentContext"/> to a view type via the <see cref="When"/> predicate.
/// Declare in XAML inside ChatOverlayControl.ContentTemplates.
/// </summary>
public abstract class ContentTemplate : BindableObject
{
    public static readonly BindableProperty ViewTypeProperty =
        BindableProperty.Create(nameof(ViewType), typeof(Type), typeof(ContentTemplate));

    public Type? ViewType
    {
        get => (Type?)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    /// <summary>Return true if this mapping should handle the given content.</summary>
    public abstract bool When(ContentContext context);

    private DataTemplate? _cachedTemplate;

    /// <summary>
    /// Returns a cached DataTemplate for the ViewType. MAUI requires the same
    /// instance on repeated calls to avoid memory leaks and broken virtualization.
    /// Subclasses can override to customize template creation (e.g. to compose wrapper + inner content).
    /// </summary>
    internal virtual DataTemplate GetTemplate()
    {
        var type = ViewType ?? throw new InvalidOperationException($"{GetType().Name} has no ViewType set.");
        return _cachedTemplate ??= new DataTemplate(type);
    }
}

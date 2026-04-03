namespace MauiAIAnnotations.Maui.Chat;

[ContentProperty(nameof(Templates))]
public class ContentTemplateSelector : DataTemplateSelector
{
    private static readonly DataTemplate FallbackTemplate =
        new(() => new Label { Text = "?" });

    public IList<ContentTemplateMapping> Templates { get; } = new List<ContentTemplateMapping>();

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ContentContext context)
        {
            foreach (var mapping in Templates)
            {
                if (mapping.When(context))
                    return mapping.GetTemplate();
            }
        }
        return FallbackTemplate;
    }
}

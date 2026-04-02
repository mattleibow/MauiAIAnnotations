namespace MauiAIAnnotations.Maui.Chat;

[ContentProperty(nameof(Templates))]
public class ContentTemplateSelector : DataTemplateSelector
{
    public IList<ContentTemplateMapping> Templates { get; } = new List<ContentTemplateMapping>();

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ContentContext context)
        {
            foreach (var mapping in Templates)
            {
                if (mapping.When(context) && mapping.ViewType is not null)
                    return mapping.CreateTemplate();
            }
        }
        return new DataTemplate(() => new Label { Text = item?.ToString() ?? "" });
    }
}

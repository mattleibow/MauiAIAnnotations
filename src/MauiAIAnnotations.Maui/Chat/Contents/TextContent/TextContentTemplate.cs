using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class TextContentTemplate : ContentTemplate
{
    public string? Role { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is TextContent &&
        (Role is null || string.Equals(context.Role, Role, StringComparison.OrdinalIgnoreCase));

    internal override DataTemplate GetTemplate()
    {
        if (ViewType is not null)
            return base.GetTemplate();

        return _cachedTemplate ??= new DataTemplate(typeof(ChatMessageView));
    }

    private DataTemplate? _cachedTemplate;
}

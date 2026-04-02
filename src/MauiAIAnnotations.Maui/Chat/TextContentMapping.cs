using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class TextContentMapping : ContentTemplateMapping
{
    public string? Role { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is TextContent &&
        (Role is null || string.Equals(context.Role, Role, StringComparison.OrdinalIgnoreCase));
}

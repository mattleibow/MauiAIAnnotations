using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class ErrorContentMapping : ContentTemplateMapping
{
    public override bool When(ContentContext context) =>
        context.Content is ErrorContent;
}

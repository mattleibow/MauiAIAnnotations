using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class ErrorContentTemplate : ContentTemplate
{
    public ErrorContentTemplate() => ViewType = typeof(ErrorMessageView);

    public override bool When(ContentContext context) =>
        context.Content is ErrorContent;
}

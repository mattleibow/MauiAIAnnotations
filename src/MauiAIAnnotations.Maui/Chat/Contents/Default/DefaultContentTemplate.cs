namespace MauiAIAnnotations.Maui.Chat;

public class DefaultContentTemplate : ContentTemplate
{
    public DefaultContentTemplate() => ViewType = typeof(DefaultContentView);

    public override bool When(ContentContext context) => true;
}

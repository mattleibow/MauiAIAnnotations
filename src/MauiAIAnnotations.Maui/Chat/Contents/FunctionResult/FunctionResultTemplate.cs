using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionResultTemplate : ContentTemplate
{
    public FunctionResultTemplate() => ViewType = typeof(FunctionResultMessageView);

    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionResultContent;
}

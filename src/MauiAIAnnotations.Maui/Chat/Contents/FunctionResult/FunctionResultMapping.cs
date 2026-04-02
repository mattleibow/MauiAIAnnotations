using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionResultMapping : ContentTemplateMapping
{
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionResultContent;
}

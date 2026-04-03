using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionCallTemplate : ContentTemplate
{
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionCallContent call &&
        (ToolName is null || string.Equals(call.Name, ToolName, StringComparison.OrdinalIgnoreCase));
}

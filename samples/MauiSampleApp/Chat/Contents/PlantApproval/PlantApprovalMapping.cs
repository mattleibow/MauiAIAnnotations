using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Matches approval requests for the add_plant tool specifically.
/// Must be registered before the generic ToolApprovalMapping in the template list.
/// </summary>
public class PlantApprovalMapping : ContentTemplateMapping
{
    public override bool When(ContentContext context) =>
        context.Content is ToolApprovalRequestContent approval &&
        approval.ToolCall is FunctionCallContent fc &&
        string.Equals(fc.Name, "add_plant", StringComparison.OrdinalIgnoreCase);
}

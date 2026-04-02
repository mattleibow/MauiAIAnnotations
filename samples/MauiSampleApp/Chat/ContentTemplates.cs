using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Base class for content template mappings. Each mapping defines a <see cref="When"/>
/// predicate and a <see cref="Template"/> to use when matched. The template selector
/// iterates mappings in order and uses the first match.
/// </summary>
public abstract class ContentTemplateMapping : BindableObject
{
    public DataTemplate? Template { get; set; }

    /// <summary>
    /// Determines if this template should handle the given content context.
    /// </summary>
    public abstract bool When(ContentContext context);
}

/// <summary>
/// Matches <see cref="TextContent"/> items, optionally filtered by role.
/// </summary>
public class TextContentMapping : ContentTemplateMapping
{
    /// <summary>
    /// If set, only matches text content with this role (e.g. "User", "Assistant").
    /// </summary>
    public string? Role { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is TextContent &&
        (Role is null || string.Equals(context.Role, Role, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Matches <see cref="FunctionCallContent"/> items, optionally filtered by tool name.
/// </summary>
public class FunctionCallMapping : ContentTemplateMapping
{
    /// <summary>
    /// If set, only matches function calls to this specific tool.
    /// </summary>
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionCallContent call &&
        (ToolName is null || string.Equals(call.Name, ToolName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Matches <see cref="FunctionResultContent"/> items, optionally filtered by tool name.
/// </summary>
public class FunctionResultMapping : ContentTemplateMapping
{
    /// <summary>
    /// If set, only matches results from this specific tool.
    /// </summary>
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionResultContent result &&
        (ToolName is null || string.Equals(result.CallId, ToolName, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Matches <see cref="ErrorContent"/> items.
/// </summary>
public class ErrorContentMapping : ContentTemplateMapping
{
    public override bool When(ContentContext context) =>
        context.Content is ErrorContent;
}

/// <summary>
/// Fallback mapping that matches any content.
/// </summary>
public class DefaultContentMapping : ContentTemplateMapping
{
    public override bool When(ContentContext context) => true;
}

/// <summary>
/// A <see cref="DataTemplateSelector"/> that holds an ordered list of
/// <see cref="ContentTemplateMapping"/> items and returns the first matching template.
/// </summary>
[ContentProperty(nameof(Templates))]
public class ContentTemplateSelector : DataTemplateSelector
{
    public IList<ContentTemplateMapping> Templates { get; } = new List<ContentTemplateMapping>();

    public DataTemplate? FallbackTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ContentContext context)
        {
            foreach (var mapping in Templates)
            {
                if (mapping.When(context) && mapping.Template is not null)
                    return mapping.Template;
            }
        }

        return FallbackTemplate ?? new DataTemplate(() => new Label { Text = item?.ToString() ?? "" });
    }
}

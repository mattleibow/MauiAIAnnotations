using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionCallTemplate : ContentTemplate
{
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionCallContent call &&
        (ToolName is null || string.Equals(call.Name, ToolName, StringComparison.OrdinalIgnoreCase));

    internal override DataTemplate GetTemplate()
    {
        if (ViewType is not null)
            return base.GetTemplate();

        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var view = new FunctionCallMessageView();
            view.SetDynamicResource(ContentView.ControlTemplateProperty, "MauiAI.FunctionCallTemplate");
            return view;
        });
    }

    private DataTemplate? _cachedTemplate;
}

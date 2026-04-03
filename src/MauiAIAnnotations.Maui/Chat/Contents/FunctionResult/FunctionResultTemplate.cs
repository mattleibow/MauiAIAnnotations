using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionResultTemplate : ContentTemplate
{
    public string? ToolName { get; set; }

    public override bool When(ContentContext context) =>
        context.Content is FunctionResultContent;

    internal override DataTemplate GetTemplate()
    {
        if (ViewType is not null)
            return base.GetTemplate();

        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var view = new FunctionResultMessageView();
            view.SetDynamicResource(ContentView.ControlTemplateProperty, "MauiAI.FunctionResultTemplate");
            return view;
        });
    }

    private DataTemplate? _cachedTemplate;
}

using Microsoft.Extensions.AI.Maui.Themes;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI.Maui.Chat;

public class ErrorContentTemplate : ContentTemplate
{
    public override bool When(ContentContext context) =>
        context.Content is ErrorContent;

    internal override DataTemplate GetTemplate()
    {
        if (ViewType is not null)
            return base.GetTemplate();

        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var view = new ErrorMessageView();
            view.SetDynamicResource(ContentView.ControlTemplateProperty, ChatThemeKeys.ErrorTemplate);
            return PrepareDataTemplateView(view);
        });
    }

    private DataTemplate? _cachedTemplate;
}

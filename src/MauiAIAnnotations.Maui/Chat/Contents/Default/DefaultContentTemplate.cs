using MauiAIAnnotations.Maui.Themes;

namespace MauiAIAnnotations.Maui.Chat;

public class DefaultContentTemplate : ContentTemplate
{
    public override bool When(ContentContext context) => true;

    internal override DataTemplate GetTemplate()
    {
        if (ViewType is not null)
            return base.GetTemplate();

        return _cachedTemplate ??= new DataTemplate(() =>
        {
            var view = new DefaultMessageView();
            view.SetDynamicResource(ContentView.ControlTemplateProperty, ChatThemeKeys.DefaultTemplate);
            return view;
        });
    }

    internal override int GetPriority(ContentContext context) =>
        base.GetPriority(context) - 1000;

    private DataTemplate? _cachedTemplate;
}

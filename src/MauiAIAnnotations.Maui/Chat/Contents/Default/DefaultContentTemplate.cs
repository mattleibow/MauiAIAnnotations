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
            view.SetDynamicResource(ContentView.ControlTemplateProperty, "MauiAI.DefaultTemplate");
            return view;
        });
    }

    private DataTemplate? _cachedTemplate;
}

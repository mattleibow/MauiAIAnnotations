namespace MauiAIAnnotations.Maui.Chat;

public partial class DefaultContentView : ContentView
{
    public DefaultContentView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new DefaultContentViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

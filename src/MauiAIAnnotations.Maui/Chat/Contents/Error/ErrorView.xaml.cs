namespace MauiAIAnnotations.Maui.Chat;

public partial class ErrorView : ContentView
{
    public ErrorView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new ErrorViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

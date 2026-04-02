namespace MauiAIAnnotations.Maui.Chat;

public partial class FunctionCallView : ContentView
{
    public FunctionCallView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new FunctionCallViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

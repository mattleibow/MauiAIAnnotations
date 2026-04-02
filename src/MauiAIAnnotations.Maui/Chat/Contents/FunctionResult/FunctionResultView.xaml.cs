namespace MauiAIAnnotations.Maui.Chat;

public partial class FunctionResultView : ContentView
{
    public FunctionResultView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new FunctionResultViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

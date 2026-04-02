namespace MauiAIAnnotations.Maui.Chat;

public partial class AssistantTextView : ContentView
{
    public AssistantTextView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new AssistantTextViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

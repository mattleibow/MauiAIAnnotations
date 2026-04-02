namespace MauiAIAnnotations.Maui.Chat;

public partial class UserTextView : ContentView
{
    public UserTextView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new UserTextViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

using MauiAIAnnotations.Maui.Chat;

namespace MauiSampleApp.Chat;

public partial class PlantResultView : ContentView
{
    private PlantResultViewModel? _vm;

    public PlantResultView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            // Reuse the ViewModel instance across recycling
            _vm ??= new PlantResultViewModel();
            _vm.SetContext(context);
            InnerContent.BindingContext = _vm;
        }
    }
}

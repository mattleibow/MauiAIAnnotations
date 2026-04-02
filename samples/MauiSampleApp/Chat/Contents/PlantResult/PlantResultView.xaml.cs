using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantResultView : ContentView
{
    public PlantResultView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext context)
        {
            var vm = new PlantResultViewModel();
            vm.SetContext(context);
            InnerContent.BindingContext = vm;
        }
    }
}

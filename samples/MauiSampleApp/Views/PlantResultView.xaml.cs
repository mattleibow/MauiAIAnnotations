using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Views;

public partial class PlantResultView : ContentView
{
    public PlantResultView() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is ContentContext ctx && ctx.Content is FunctionResultContent result)
        {
            var plant = PlantResultMapping.TryGetPlant(result);
            if (plant is not null)
                Card.BindingContext = plant;
        }
    }
}

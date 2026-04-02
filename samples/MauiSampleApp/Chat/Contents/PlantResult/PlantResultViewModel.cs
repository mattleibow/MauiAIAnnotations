using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantResultViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Plant? Plant { get; set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            Plant = PlantResultMapping.TryGetPlant(result);
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantResultViewModel : ObservableObject, IContentContextAware
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSinglePlant))]
    [NotifyPropertyChangedFor(nameof(HasMultiplePlants))]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    public partial Plant? Plant { get; set; }

    public ObservableCollection<Plant> Plants { get; } = [];

    public bool HasSinglePlant => Plant is not null && Plants.Count == 1;

    public bool HasMultiplePlants => Plants.Count > 1;

    public string PreviewTitle => Plants.Count switch
    {
        0 => string.Empty,
        1 => "Plant preview",
        _ => $"{Plants.Count} plants found"
    };

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            var plants = PlantResultTemplate.TryGetPlants(result);

            Plants.Clear();
            foreach (var plant in plants)
                Plants.Add(plant);

            Plant = Plants.FirstOrDefault();
            OnPropertyChanged(nameof(HasSinglePlant));
            OnPropertyChanged(nameof(HasMultiplePlants));
            OnPropertyChanged(nameof(PreviewTitle));
        }
    }
}

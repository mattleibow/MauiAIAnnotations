using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantResultViewModel : ObservableObject, IContentContextAware
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSinglePlant))]
    [NotifyPropertyChangedFor(nameof(HasMultiplePlants))]
    [NotifyPropertyChangedFor(nameof(HasNoPlants))]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(EmptyStateText))]
    public partial Plant? Plant { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(EmptyStateText))]
    public partial string? ToolName { get; set; }

    public ObservableCollection<Plant> Plants { get; } = [];

    public bool HasSinglePlant => Plant is not null && Plants.Count == 1;

    public bool HasMultiplePlants => Plants.Count > 1;

    public bool HasNoPlants => Plants.Count == 0;

    public string PreviewTitle => Plants.Count switch
    {
        0 => string.Empty,
        1 when string.Equals(ToolName, PlantResultTemplate.GetPlantsToolName, StringComparison.OrdinalIgnoreCase) => "1 plant found",
        1 => "Plant preview",
        _ => $"{Plants.Count} plants found"
    };

    public string EmptyStateText => ToolName switch
    {
        PlantResultTemplate.GetPlantToolName => "I couldn't find that plant in your garden yet.",
        PlantResultTemplate.GetPlantsToolName => "No plants matched that request.",
        _ => "No plant details are available for this result."
    };

    public void ApplyContentContext(ContentContext context)
    {
        ToolName = context.ToolName;

        if (context.Content is FunctionResultContent result)
        {
            var plants = PlantResultTemplate.TryGetPlants(result);

            Plants.Clear();
            foreach (var plant in plants)
                Plants.Add(plant);

            Plant = Plants.FirstOrDefault();
            OnPropertyChanged(nameof(HasSinglePlant));
            OnPropertyChanged(nameof(HasMultiplePlants));
            OnPropertyChanged(nameof(HasNoPlants));
            OnPropertyChanged(nameof(PreviewTitle));
            OnPropertyChanged(nameof(EmptyStateText));
        }
    }
}

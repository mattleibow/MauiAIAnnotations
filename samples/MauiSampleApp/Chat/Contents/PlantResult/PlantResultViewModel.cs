using System.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public class PlantResultViewModel : INotifyPropertyChanged
{
    public Plant? Plant { get; private set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            Plant = PlantResultMapping.TryGetPlant(result);
            OnPropertyChanged(nameof(Plant));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

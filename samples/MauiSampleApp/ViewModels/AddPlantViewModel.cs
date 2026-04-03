using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public partial class AddPlantViewModel(PlantDataService plantDataService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Nickname { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Species { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; }

    [ObservableProperty]
    public partial bool IsIndoor { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    private bool CanSave() => !IsBusy && !string.IsNullOrWhiteSpace(Nickname) && !string.IsNullOrWhiteSpace(Species);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = "Adding plant and looking up species info...";
        try
        {
            await plantDataService.AddPlantAsync(new MauiSampleApp.Core.Models.NewPlantRequest
            {
                Nickname = Nickname,
                Species = Species,
                Location = Location,
                IsIndoor = IsIndoor
            });
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

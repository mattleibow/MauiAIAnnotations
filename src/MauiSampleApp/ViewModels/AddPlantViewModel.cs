using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class AddPlantViewModel : INotifyPropertyChanged
{
    private readonly PlantDataService _plantDataService;

    public AddPlantViewModel(PlantDataService plantDataService)
    {
        _plantDataService = plantDataService;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Nickname) && !string.IsNullOrWhiteSpace(Species));
    }

    private string _nickname = string.Empty;
    public string Nickname
    {
        get => _nickname;
        set { _nickname = value; OnPropertyChanged(); ((Command)SaveCommand).ChangeCanExecute(); }
    }

    private string _species = string.Empty;
    public string Species
    {
        get => _species;
        set { _species = value; OnPropertyChanged(); ((Command)SaveCommand).ChangeCanExecute(); }
    }

    private string _location = string.Empty;
    public string Location
    {
        get => _location;
        set { _location = value; OnPropertyChanged(); }
    }

    private bool _isIndoor;
    public bool IsIndoor
    {
        get => _isIndoor;
        set { _isIndoor = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); ((Command)SaveCommand).ChangeCanExecute(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = "Adding plant and looking up species info...";
        try
        {
            await _plantDataService.AddPlantAsync(Nickname, Species, Location, IsIndoor);
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

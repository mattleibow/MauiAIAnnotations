using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class AddPlantViewModel(PlantDataService plantDataService) : INotifyPropertyChanged
{
    private Command? _saveCommand;

    private string _nickname = string.Empty;
    public string Nickname
    {
        get => _nickname;
        set { _nickname = value; OnPropertyChanged(); SaveCmd.ChangeCanExecute(); }
    }

    private string _species = string.Empty;
    public string Species
    {
        get => _species;
        set { _species = value; OnPropertyChanged(); SaveCmd.ChangeCanExecute(); }
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
        set { _isBusy = value; OnPropertyChanged(); SaveCmd.ChangeCanExecute(); }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private Command SaveCmd => _saveCommand ??= new Command(async () => await SaveAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(Nickname) && !string.IsNullOrWhiteSpace(Species));
    public ICommand SaveCommand => SaveCmd;

    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = "Adding plant and looking up species info...";
        try
        {
            await plantDataService.AddPlantAsync(Nickname, Species, Location, IsIndoor);
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

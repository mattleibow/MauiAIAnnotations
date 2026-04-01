using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class WeatherViewModel : INotifyPropertyChanged
{
    private readonly WeatherService _weatherService;
    private readonly GeocodingService _geocodingService;
    private string _locationText = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public WeatherViewModel(WeatherService weatherService, GeocodingService geocodingService)
    {
        _weatherService = weatherService;
        _geocodingService = geocodingService;
        SearchCommand = new Command(async () => await SearchWeatherAsync(), () => !IsLoading);
    }

    public ObservableCollection<WeatherDay> WeatherDays { get; } = [];

    public string LocationText
    {
        get => _locationText;
        set => SetProperty(ref _locationText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetProperty(ref _isLoading, value);
            ((Command)SearchCommand).ChangeCanExecute();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand SearchCommand { get; }

    private async Task SearchWeatherAsync()
    {
        if (string.IsNullOrWhiteSpace(LocationText))
        {
            StatusMessage = "Please enter a location.";
            return;
        }

        IsLoading = true;
        StatusMessage = $"Searching weather for {LocationText}...";
        WeatherDays.Clear();

        try
        {
            var coords = await _geocodingService.GeocodeAsync(LocationText);

            if (coords is null)
            {
                StatusMessage = $"Could not find location: {LocationText}";
                return;
            }

            var days = await _weatherService.GetWeatherByCoordinatesAsync(coords.Value.Latitude, coords.Value.Longitude);

            if (days.Count == 0)
            {
                StatusMessage = "No weather data available.";
                return;
            }

            foreach (var day in days)
                WeatherDays.Add(day);

            StatusMessage = $"Weather for {LocationText} ({coords.Value.Latitude:F2}, {coords.Value.Longitude:F2})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

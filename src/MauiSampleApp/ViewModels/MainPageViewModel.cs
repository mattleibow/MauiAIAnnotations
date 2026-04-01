using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiSampleApp.Core.Models;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly WeatherService _weatherService;
    private string _locationQuery = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public MainPageViewModel(WeatherService weatherService)
    {
        _weatherService = weatherService;
        WeatherItems = [];
        SearchCommand = new Command(async () => await SearchWeatherAsync(), () => !IsLoading);
    }

    public ObservableCollection<DailyWeatherItem> WeatherItems { get; }

    public string LocationQuery
    {
        get => _locationQuery;
        set { _locationQuery = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
            ((Command)SearchCommand).ChangeCanExecute();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand SearchCommand { get; }

    private async Task SearchWeatherAsync()
    {
        if (string.IsNullOrWhiteSpace(LocationQuery))
            return;

        IsLoading = true;
        StatusMessage = string.Empty;
        WeatherItems.Clear();

        try
        {
            // Try MAUI Essentials Geocoding first to convert location string to coordinates
            List<DailyWeatherItem> items;
            try
            {
                var locations = await Microsoft.Maui.Devices.Sensors.Geocoding.Default.GetLocationsAsync(LocationQuery);
                var location = locations?.FirstOrDefault();

                if (location is not null)
                {
                    items = await _weatherService.GetWeatherForecastAsync(location.Latitude, location.Longitude);
                }
                else
                {
                    // MAUI Essentials returned no results — fallback to open-meteo geocoding
                    items = await _weatherService.GetWeatherForecastAsync(LocationQuery);
                }
            }
            catch
            {
                // MAUI Essentials Geocoding not available (e.g., no MapServiceToken on Windows)
                // Fallback to the weather service's location-based method (open-meteo geocoding)
                items = await _weatherService.GetWeatherForecastAsync(LocationQuery);
            }

            if (items.Count == 0)
            {
                StatusMessage = "No weather data found for this location.";
            }
            else
            {
                foreach (var item in items)
                    WeatherItems.Add(item);
            }
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

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

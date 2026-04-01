using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public record ChatMessage(string Role, string Content);

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient _chatClient;
    private readonly WeatherService _weatherService;
    private string _userInput = string.Empty;
    private bool _isBusy;

    public ChatViewModel(IChatClient chatClient, WeatherService weatherService)
    {
        _chatClient = chatClient;
        _weatherService = weatherService;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
        SetupAIFunctions();
    }

    public ObservableCollection<ChatMessage> Messages { get; }

    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public ICommand SendCommand { get; }

    private List<AITool> _aiTools = [];

    private void SetupAIFunctions()
    {
        var getWeatherByLocation = AIFunctionFactory.Create(
            async (string location) =>
            {
                var items = await _weatherService.GetWeatherForecastAsync(location);
                if (items.Count == 0)
                    return "No weather data available for this location.";

                var lines = items.Select(i => $"{i.Date}: {i.Emoji} {i.Temperature:F0}°C - {i.Description}");
                return $"7-day weather forecast for {location}:\n" + string.Join("\n", lines);
            },
            "get_weather_by_location",
            "Gets the 7-day weather forecast for a given city or location name");

        var getWeatherByCoordinates = AIFunctionFactory.Create(
            async (double latitude, double longitude) =>
            {
                var items = await _weatherService.GetWeatherForecastAsync(latitude, longitude);
                if (items.Count == 0)
                    return "No weather data available for these coordinates.";

                var lines = items.Select(i => $"{i.Date}: {i.Emoji} {i.Temperature:F0}°C - {i.Description}");
                return $"7-day weather forecast for ({latitude:F4}, {longitude:F4}):\n" + string.Join("\n", lines);
            },
            "get_weather_by_coordinates",
            "Gets the 7-day weather forecast for a given latitude and longitude");

        _aiTools = [getWeatherByLocation, getWeatherByCoordinates];
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput;
        UserInput = string.Empty;
        Messages.Add(new ChatMessage("User", userMessage));
        IsBusy = true;

        try
        {
            var history = Messages
                .Select(m => m.Role == "User"
                    ? new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, m.Content)
                    : new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, m.Content))
                .ToList();

            var options = new ChatOptions
            {
                Tools = _aiTools
            };

            var response = await _chatClient.GetResponseAsync(history, options);
            Messages.Add(new ChatMessage("Assistant", response.Text ?? "(no response)"));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage("Assistant", $"Error: {ex.Message}"));
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

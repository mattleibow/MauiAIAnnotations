using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient? _chatClient;
    private readonly WeatherService _weatherService;
    private readonly GeocodingService _geocodingService;
    private string _userInput = string.Empty;
    private bool _isSending;
    private bool _isChatVisible;
    private readonly List<ChatMessage> _chatHistory = [];

    public ChatViewModel(WeatherService weatherService, GeocodingService geocodingService, IChatClient? chatClient = null)
    {
        _weatherService = weatherService;
        _geocodingService = geocodingService;
        _chatClient = chatClient;
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsSending);
        ToggleChatCommand = new Command(() => IsChatVisible = !IsChatVisible);

        _chatHistory.Add(new ChatMessage(ChatRole.System,
            "You are a helpful weather assistant. You can look up weather forecasts for any location. " +
            "When a user asks about weather, use the available tools to get the forecast. " +
            "Be concise and friendly in your responses."));
    }

    public ObservableCollection<ChatMessageDisplay> Messages { get; } = [];

    public string UserInput
    {
        get => _userInput;
        set => SetProperty(ref _userInput, value);
    }

    public bool IsSending
    {
        get => _isSending;
        set
        {
            SetProperty(ref _isSending, value);
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public bool IsChatVisible
    {
        get => _isChatVisible;
        set => SetProperty(ref _isChatVisible, value);
    }

    public ICommand SendCommand { get; }
    public ICommand ToggleChatCommand { get; }

    private IList<AITool> GetWeatherTools()
    {
        var getWeatherByLocation = AIFunctionFactory.Create(
            async (string location) =>
            {
                var result = await _weatherService.GetWeatherByLocationAsync(location, async loc =>
                {
                    return await _geocodingService.GeocodeAsync(loc);
                });
                return result;
            },
            "get_weather_by_location",
            "Gets a 7-day weather forecast for a location name (city, place, address). Returns weather with emoji, temperature, and description for each day.");

        var getWeatherByCoordinates = AIFunctionFactory.Create(
            async (double latitude, double longitude) =>
            {
                var days = await _weatherService.GetWeatherByCoordinatesAsync(latitude, longitude);
                if (days.Count == 0) return "Weather unavailable for these coordinates.";
                return string.Join("\n", days.Select(d =>
                    $"{d.Date:ddd MMM dd}: {d.Emoji} {d.Temperature:F0}°C - {d.Description}"));
            },
            "get_weather_by_coordinates",
            "Gets a 7-day weather forecast for specific GPS coordinates (latitude, longitude). Returns weather with emoji, temperature, and description for each day.");

        return [getWeatherByLocation, getWeatherByCoordinates];
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput) || _chatClient is null)
            return;

        var userText = UserInput.Trim();
        UserInput = string.Empty;

        Messages.Add(new ChatMessageDisplay("You", userText, true));
        _chatHistory.Add(new ChatMessage(ChatRole.User, userText));

        IsSending = true;
        Messages.Add(new ChatMessageDisplay("AI", "Thinking...", false));

        try
        {
            var options = new ChatOptions
            {
                Tools = GetWeatherTools()
            };

            var response = await _chatClient.GetResponseAsync(_chatHistory, options);

            // Remove "Thinking..." placeholder
            Messages.RemoveAt(Messages.Count - 1);

            var assistantText = response.Text ?? "I couldn't generate a response.";
            Messages.Add(new ChatMessageDisplay("AI", assistantText, false));
            _chatHistory.Add(new ChatMessage(ChatRole.Assistant, assistantText));
        }
        catch (Exception ex)
        {
            Messages.RemoveAt(Messages.Count - 1);
            Messages.Add(new ChatMessageDisplay("AI", $"Error: {ex.Message}", false));
        }
        finally
        {
            IsSending = false;
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

public record ChatMessageDisplay(string Sender, string Text, bool IsUser);

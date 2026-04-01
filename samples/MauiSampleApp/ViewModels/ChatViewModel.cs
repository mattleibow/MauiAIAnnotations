using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Services;

namespace MauiSampleApp.ViewModels;

public record ConversationEntry(string Role, string Content);

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient _chatClient;
    private readonly SpeciesService _speciesService;
    private readonly PlantDataService _plantDataService;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private static readonly string SystemPrompt = """
        You are a friendly gardening assistant. You help users manage their plants, track care events, and answer plant care questions.
        You have access to tools to look up species information, manage plants, and log care events.
        When a user mentions adding a plant, use the AddPlant tool. When they ask about care, look up the species profile and care history.
        Be conversational and helpful. Use emoji occasionally to be friendly 🌱
        """;

    public ChatViewModel(IChatClient chatClient, SpeciesService speciesService, PlantDataService plantDataService)
    {
        _chatClient = chatClient;
        _speciesService = speciesService;
        _plantDataService = plantDataService;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
        SetupAIFunctions();
    }

    public ObservableCollection<ConversationEntry> Messages { get; }

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
        var getSpecies = AIFunctionFactory.Create(
            async (string name) =>
            {
                var species = await _speciesService.GetSpeciesAsync(name);
                return JsonSerializer.Serialize(species);
            },
            "get_species",
            "Gets a species profile by common name (e.g. 'tomato', 'basil'). Returns care information including watering frequency, sunlight needs, and frost tolerance.");

        var getPlants = AIFunctionFactory.Create(
            async () =>
            {
                var plants = await _plantDataService.GetPlantsAsync();
                return JsonSerializer.Serialize(plants);
            },
            "get_plants",
            "Gets all plants the user has registered.");

        var getPlant = AIFunctionFactory.Create(
            async (string nickname) =>
            {
                var plant = await _plantDataService.GetPlantAsync(nickname);
                return plant is not null ? JsonSerializer.Serialize(plant) : "Plant not found.";
            },
            "get_plant",
            "Gets a specific plant by its nickname.");

        var addPlant = AIFunctionFactory.Create(
            async (string nickname, string species, string location, bool isIndoor) =>
            {
                var plant = await _plantDataService.AddPlantAsync(nickname, species, location, isIndoor);
                return $"Added plant '{plant.Nickname}' ({species}) at {plant.Location}.";
            },
            "add_plant",
            "Adds a new plant. Requires nickname, species name, location, and whether it's indoors.");

        var removePlant = AIFunctionFactory.Create(
            async (string nickname) =>
            {
                await _plantDataService.RemovePlantAsync(nickname);
                return $"Removed plant '{nickname}'.";
            },
            "remove_plant",
            "Removes a plant by its nickname.");

        var logCareEvent = AIFunctionFactory.Create(
            async (string plantNickname, string eventType, string notes) =>
            {
                var careEvent = await _plantDataService.LogCareEventAsync(plantNickname, eventType, notes);
                return $"Logged '{eventType}' for '{plantNickname}' at {careEvent.Timestamp:g}.";
            },
            "log_care_event",
            "Logs a care event for a plant. EventType must be one of: Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed.");

        var getCareHistory = AIFunctionFactory.Create(
            async (string plantNickname) =>
            {
                var history = await _plantDataService.GetCareHistoryAsync(plantNickname);
                return history.Count > 0
                    ? JsonSerializer.Serialize(history)
                    : $"No care history found for '{plantNickname}'.";
            },
            "get_care_history",
            "Gets the care history for a plant by its nickname.");

        _aiTools = [getSpecies, getPlants, getPlant, addPlant, removePlant, logCareEvent, getCareHistory];
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var userMessage = UserInput;
        UserInput = string.Empty;
        Messages.Add(new ConversationEntry("User", userMessage));
        IsBusy = true;

        try
        {
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };

            foreach (var m in Messages)
            {
                history.Add(m.Role == "User"
                    ? new ChatMessage(ChatRole.User, m.Content)
                    : new ChatMessage(ChatRole.Assistant, m.Content));
            }

            var options = new ChatOptions { Tools = _aiTools };

            // Streaming response
            var index = Messages.Count;
            Messages.Add(new ConversationEntry("Assistant", ""));
            var responseText = "";

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                if (update.Text is { } text)
                {
                    responseText += text;
                    Messages[index] = new ConversationEntry("Assistant", responseText);
                }
            }

            if (string.IsNullOrEmpty(responseText))
                Messages[index] = new ConversationEntry("Assistant", "(no response)");
        }
        catch (Exception ex)
        {
            Messages.Add(new ConversationEntry("Assistant", $"Error: {ex.Message}"));
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

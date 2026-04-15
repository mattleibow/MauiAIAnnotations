using System.Text.Json;
using Microsoft.Extensions.AI.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Matches plant-oriented tool results and renders either a single plant,
/// a multi-plant preview, or a friendly empty state when nothing matched.
/// </summary>
public class PlantResultTemplate : FunctionResultTemplate
{
    public const string AddPlantToolName = "add_plant";
    public const string GetPlantToolName = "get_plant";
    public const string GetPlantsToolName = "get_plants";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override bool When(ContentContext context)
    {
        if (!base.When(context) || context.Content is not FunctionResultContent result)
            return false;

        return IsPlantTool(context.ToolName) || TryGetPlants(result).Count > 0;
    }

    public static Plant? TryGetPlant(FunctionResultContent result) =>
        TryGetPlants(result).FirstOrDefault();

    public static IReadOnlyList<Plant> TryGetPlants(FunctionResultContent result)
    {
        if (result.Result is Plant plant)
            return IsValidPlant(plant) ? [plant] : [];

        if (result.Result is IEnumerable<Plant> plants)
            return plants.Where(IsValidPlant).ToList();

        if (result.Result is JsonElement json)
        {
            return TryDeserializeJson(json);
        }

        if (result.Result is string str)
        {
            return TryDeserializeString(str);
        }

        return [];
    }

    private static IReadOnlyList<Plant> TryDeserializeJson(JsonElement json)
    {
        try
        {
            return json.ValueKind switch
            {
                JsonValueKind.Object => JsonSerializer.Deserialize<Plant>(json.GetRawText(), SerializerOptions) is { } plant
                    && IsValidPlant(plant)
                    ? [plant]
                    : [],
                JsonValueKind.Array => (JsonSerializer.Deserialize<List<Plant>>(json.GetRawText(), SerializerOptions) ?? [])
                    .Where(IsValidPlant)
                    .ToList(),
                _ => []
            };
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<Plant> TryDeserializeString(string value)
    {
        var trimmed = value.TrimStart();
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return [];

        try
        {
            if (trimmed.StartsWith('{'))
            {
                return JsonSerializer.Deserialize<Plant>(value, SerializerOptions) is { } plant
                    && IsValidPlant(plant)
                    ? [plant]
                    : [];
            }

            return (JsonSerializer.Deserialize<List<Plant>>(value, SerializerOptions) ?? [])
                .Where(IsValidPlant)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsValidPlant(Plant? plant) =>
        plant is not null &&
        !string.IsNullOrWhiteSpace(plant.Nickname);

    private static bool IsPlantTool(string? toolName) =>
        string.Equals(toolName, AddPlantToolName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(toolName, GetPlantToolName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(toolName, GetPlantsToolName, StringComparison.OrdinalIgnoreCase);
}

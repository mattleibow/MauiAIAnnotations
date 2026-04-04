using System.Text.Json;
using MauiAIAnnotations.Maui.Chat;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Matches <see cref="FunctionResultContent"/> where the result is a <see cref="Plant"/>
/// or a list of <see cref="Plant"/> objects.
/// </summary>
public class PlantResultTemplate : ContentTemplate
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override bool When(ContentContext context)
    {
        if (context.Content is not FunctionResultContent result)
            return false;

        return TryGetPlants(result).Count > 0;
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
}

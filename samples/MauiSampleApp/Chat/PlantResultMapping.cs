using System.Text.Json;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Matches <see cref="FunctionResultContent"/> where the result is a <see cref="Plant"/> object.
/// This template takes priority over the generic <see cref="FunctionResultMapping"/> when placed
/// before it in the template list, demonstrating tool-specific result rendering.
/// </summary>
public class PlantResultMapping : ContentTemplateMapping
{
    public override bool When(ContentContext context)
    {
        if (context.Content is not FunctionResultContent result)
            return false;

        return TryGetPlant(result) is not null;
    }

    /// <summary>
    /// Attempts to extract a <see cref="Plant"/> from a <see cref="FunctionResultContent"/>.
    /// </summary>
    internal static Plant? TryGetPlant(FunctionResultContent result)
    {
        try
        {
            if (result.Result is Plant plant)
                return plant;

            if (result.Result is JsonElement json)
            {
                return JsonSerializer.Deserialize<Plant>(json.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            if (result.Result is string str && str.TrimStart().StartsWith('{'))
            {
                return JsonSerializer.Deserialize<Plant>(str, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
        }
        catch
        {
            // Not a Plant — fall through to generic template
        }

        return null;
    }
}

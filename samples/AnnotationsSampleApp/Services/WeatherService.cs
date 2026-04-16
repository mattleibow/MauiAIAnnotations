using System.ComponentModel;
using Microsoft.Extensions.AI.Attributes;

namespace AnnotationsSampleApp.Services;

/// <summary>
/// Provides seasonal growing condition advice.
/// Registered as transient — a new instance is created for every resolution.
/// Demonstrates a stateless service with a sync return type and default parameter values.
/// </summary>
public class WeatherService
{
    [Description("Gets gardening advice for the current growing conditions based on month and hemisphere.")]
    [ExportAIFunction("get_growing_conditions")]
    public string GetGrowingConditions(
        [Description("The month number (1-12)")] int month,
        [Description("The hemisphere: 'north' or 'south'. Defaults to north.")] string hemisphere = "north")
    {
        var effectiveMonth = hemisphere.Equals("south", StringComparison.OrdinalIgnoreCase)
            ? ((month + 5) % 12) + 1
            : month;

        return effectiveMonth switch
        {
            >= 3 and <= 5 => "🌱 Spring: Perfect for planting annuals and starting seeds. Watch for late frosts. Soil temperature is rising.",
            >= 6 and <= 8 => "☀️ Summer: Focus on watering and mulching. Harvest regularly. Peak growing season for warm-weather crops.",
            >= 9 and <= 11 => "🍂 Autumn: Plant bulbs for spring. Collect seeds. Add compost to beds. Prepare tender plants for winter.",
            12 or 1 or 2 => "❄️ Winter: Plan next year's garden. Order seeds. Prune dormant trees. Protect plants from frost.",
            _ => "Invalid month. Please provide a number between 1 and 12."
        };
    }
}

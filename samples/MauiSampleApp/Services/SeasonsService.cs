using System.ComponentModel;
using MauiAIAnnotations;

namespace MauiSampleApp.Services;

public class SeasonsService
{
    [ExportAIFunction("get_seasonal_gardening_advice",
        Description = "Gets gardening advice for the current season based on the month number and hemisphere. Seasons are reversed in the southern hemisphere.")]
    public string GetSeasonalAdvice(
        [Description("The month number (1-12)")] int month,
        [Description("The hemisphere: 'north' or 'south'. Defaults to north if not specified.")] string hemisphere = "north")
    {
        // In southern hemisphere, shift by 6 months to get equivalent northern season
        var effectiveMonth = hemisphere.Equals("south", StringComparison.OrdinalIgnoreCase)
            ? ((month + 5) % 12) + 1
            : month;

        return effectiveMonth switch
        {
            >= 3 and <= 5 => "Spring: Great time for planting annuals, starting seeds, and dividing perennials. Watch for late frosts.",
            >= 6 and <= 8 => "Summer: Focus on watering, mulching, and pest control. Harvest regularly to encourage more growth.",
            >= 9 and <= 11 => "Autumn: Plant bulbs for spring, collect seeds, add compost to beds, and prepare tender plants for winter.",
            12 or 1 or 2 => "Winter: Plan next year's garden, order seeds, prune dormant trees, and protect plants from frost.",
            _ => "Invalid month number. Please provide a number between 1 and 12."
        };
    }
}

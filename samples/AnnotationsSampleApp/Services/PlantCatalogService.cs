using System.ComponentModel;
using AnnotationsSampleApp.Models;
using Microsoft.Extensions.AI.Attributes;

namespace AnnotationsSampleApp.Services;

/// <summary>
/// Read-only plant catalog with static in-memory data.
/// Registered as a singleton — shared across all chat sessions.
/// </summary>
public class PlantCatalogService
{
    private static readonly List<PlantInfo> Catalog =
    [
        new("Tomato", "Solanum lycopersicum", "Vegetable", "Full Sun", 2),
        new("Basil", "Ocimum basilicum", "Herb", "Full Sun", 1),
        new("Lavender", "Lavandula angustifolia", "Herb", "Full Sun", 7),
        new("Fern", "Nephrolepis exaltata", "Houseplant", "Low Light", 3),
        new("Mint", "Mentha spicata", "Herb", "Partial Sun", 2),
        new("Sunflower", "Helianthus annuus", "Flower", "Full Sun", 3),
        new("Aloe Vera", "Aloe barbadensis", "Succulent", "Full Sun", 14),
        new("Snake Plant", "Dracaena trifasciata", "Houseplant", "Low Light", 14),
        new("Rosemary", "Salvia rosmarinus", "Herb", "Full Sun", 5),
        new("Pepper", "Capsicum annuum", "Vegetable", "Full Sun", 2),
    ];

    private static readonly Dictionary<string, CareGuide> CareGuides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tomato"] = new("Tomato",
            "Water deeply 2-3 times per week. Keep soil consistently moist but not waterlogged.",
            "Full sun, at least 6-8 hours daily.",
            "Rich, well-draining soil with pH 6.0-6.8.",
            "Feed every 2 weeks with balanced fertilizer once flowering begins.",
            false,
            ["Blossom end rot", "Tomato hornworm", "Early blight"]),
        ["basil"] = new("Basil",
            "Water daily in hot weather. Keep soil moist but not soggy.",
            "Full sun, 6-8 hours daily. Protect from harsh afternoon sun.",
            "Rich, moist, well-draining soil.",
            "Light feeding every 4-6 weeks with liquid fertilizer.",
            false,
            ["Aphids", "Fusarium wilt", "Downy mildew"]),
        ["lavender"] = new("Lavender",
            "Water sparingly — once per week at most. Drought tolerant once established.",
            "Full sun, 6+ hours daily.",
            "Sandy, well-draining, slightly alkaline soil.",
            "Minimal fertilizing. A light application in spring if needed.",
            true,
            ["Root rot from overwatering", "Lavender shab disease"]),
        ["fern"] = new("Fern",
            "Keep soil consistently moist. Mist regularly for humidity.",
            "Indirect light or shade. No direct sun.",
            "Rich, humus-heavy, well-draining soil.",
            "Feed monthly during growing season with diluted liquid fertilizer.",
            false,
            ["Brown fronds from low humidity", "Scale insects"]),
    };

    [Description("Searches the plant catalog. Returns all plants if no query is given, or filters by name/category.")]
    [ExportAIFunction("search_plants")]
    public List<PlantInfo> SearchPlants(
        [Description("Optional text to filter by common name, scientific name, or category. Leave blank for all plants.")]
        string? query = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [.. Catalog];

        var q = query.Trim();
        return Catalog
            .Where(p =>
                p.CommonName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.ScientificName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    [Description("Gets a detailed care guide for a plant species. Returns watering, sunlight, soil, and common issues.")]
    [ExportAIFunction("get_plant_care_guide")]
    public CareGuide GetPlantCareGuide(
        [Description("The common name of the plant species (e.g., 'tomato', 'basil')")]
        string species)
    {
        if (CareGuides.TryGetValue(species.Trim(), out var guide))
            return guide;

        return new CareGuide(
            species,
            "Water regularly based on soil moisture.",
            "Check species requirements.",
            "Well-draining soil.",
            "Feed during growing season.",
            false,
            ["No specific issues documented."]);
    }
}

using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class SpeciesService(IDocumentStore store, IChatClient? chatClient = null)
{
    public async Task<SpeciesProfile> GetSpeciesAsync(string name)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        // Check local cache first
        var all = await store.Query<SpeciesProfile>().ToList();
        var cached = all.FirstOrDefault(s => s.CommonName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

        if (cached is not null)
            return cached;

        // Cache miss — generate via AI and cache
        var profile = await GenerateSpeciesProfileAsync(normalizedName);
        await store.Insert(profile);
        return profile;
    }

    public async Task<SpeciesProfile?> GetSpeciesByIdAsync(string id)
    {
        return await store.Get<SpeciesProfile>(id);
    }

    private async Task<SpeciesProfile> GenerateSpeciesProfileAsync(string name)
    {
        if (chatClient is null)
            return CreateFallbackProfile(name);

        try
        {
            var prompt = $"Generate a species profile for the plant \"{name}\". Include common name, scientific name, watering frequency in days, sunlight needs (Low/Medium/Full), frost tolerance, and care tips.";
            var result = await chatClient.GetResponseAsync<SpeciesProfile>(prompt);

            if (result.Result is { } profile)
            {
                profile.Id = Guid.NewGuid().ToString();
                return profile;
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return CreateFallbackProfile(name);
    }

    private static SpeciesProfile CreateFallbackProfile(string name) => new()
    {
        Id = Guid.NewGuid().ToString(),
        CommonName = char.ToUpper(name[0]) + name[1..],
        ScientificName = $"{char.ToUpper(name[0]) + name[1..]} sp.",
        WateringFrequencyDays = 3,
        SunlightNeeds = "Full",
        FrostTolerant = false,
        Notes = $"General care tips for {name}. Water regularly and ensure adequate sunlight."
    };
}

using System.Text.Json;
using MauiSampleApp.Core.Models;
using Microsoft.Extensions.AI;
using Shiny.DocumentDb;

namespace MauiSampleApp.Core.Services;

public class SpeciesService
{
    private readonly IDocumentStore _store;
    private readonly IChatClient _chatClient;

    public SpeciesService(IDocumentStore store, IChatClient chatClient)
    {
        _store = store;
        _chatClient = chatClient;
    }

    public async Task<SpeciesProfile> GetSpeciesAsync(string name)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        // Check local cache first
        var all = await _store.Query<SpeciesProfile>().ToList();
        var cached = all.FirstOrDefault(s => s.CommonName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

        if (cached is not null)
            return cached;

        // Cache miss — generate via AI and cache
        var profile = await GenerateSpeciesProfileAsync(normalizedName);
        await _store.Insert(profile);
        return profile;
    }

    public async Task<SpeciesProfile?> GetSpeciesByIdAsync(string id)
    {
        return await _store.Get<SpeciesProfile>(id);
    }

    private async Task<SpeciesProfile> GenerateSpeciesProfileAsync(string name)
    {
        var prompt = $$"""
            Generate a species profile for the plant "{{name}}".
            Return ONLY a JSON object with these exact fields:
            {
              "CommonName": "the common name properly capitalized",
              "ScientificName": "the scientific/Latin name",
              "WateringFrequencyDays": number of days between watering,
              "SunlightNeeds": "Low" or "Medium" or "Full",
              "FrostTolerant": true or false,
              "Notes": "2-3 sentences of general care tips"
            }
            Return ONLY the JSON, no markdown, no explanation.
            """;

        try
        {
            var response = await _chatClient.GetResponseAsync(prompt);
            var text = response.Text ?? "";

            // Strip any markdown fencing the AI might add
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                var firstNewline = text.IndexOf('\n');
                if (firstNewline >= 0)
                    text = text[(firstNewline + 1)..];
                if (text.EndsWith("```"))
                    text = text[..^3];
                text = text.Trim();
            }

            var profile = JsonSerializer.Deserialize<SpeciesProfile>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (profile is not null)
            {
                profile.Id = Guid.NewGuid().ToString();
                return profile;
            }
        }
        catch
        {
            // Fall through to fallback if AI fails
        }

        // Fallback if AI parsing fails
        return new SpeciesProfile
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
}

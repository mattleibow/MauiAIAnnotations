using Microsoft.Extensions.AI.Attributes;
using MauiSampleApp.Core.Services;
using MauiSampleApp.Services;

namespace MauiSampleApp;

/// <summary>
/// Source-generated AI tool context for the garden assistant.
/// Aggregates all tools from the app's service types.
/// </summary>
[AIToolSource(typeof(PlantDataService))]
[AIToolSource(typeof(SeasonsService))]
[AIToolSource(typeof(SpeciesService))]
public partial class GardenTools : AIToolContext { }

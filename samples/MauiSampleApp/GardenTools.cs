using Microsoft.Extensions.AI.Attributes;
using MauiSampleApp.Core.Services;
using MauiSampleApp.Services;

namespace MauiSampleApp;

// AI Tool Contexts
//
// Each context declares which service types contribute AI tools.
// The source generator emits the tool registration code at compile time.
//
// Contexts can overlap (multiple contexts can reference the same service)
// and can be registered with or without a service key.

/// <summary>
/// All garden tools: the full assistant experience.
/// Includes read-only queries, write operations (with approval), and seasonal advice.
/// Registered as the default (non-keyed) tool set.
/// </summary>
[AIToolSource(typeof(PlantDataService))]
[AIToolSource(typeof(SeasonsService))]
[AIToolSource(typeof(SpeciesService))]
public partial class GardenTools : AIToolContext { }

/// <summary>
/// Read-only tools: safe for browsing without any mutations.
/// Excludes PlantDataService entirely since it contains write methods.
/// Could power a "browse my garden" chat that cannot change anything.
/// </summary>
[AIToolSource(typeof(SeasonsService))]
[AIToolSource(typeof(SpeciesService))]
public partial class ReadOnlyGardenTools : AIToolContext { }

/// <summary>
/// Plant management tools only: a focused subset of PlantDataService.
/// Registered as a keyed service ("plant-management") to demonstrate
/// how multiple tool sets can coexist and be resolved independently.
/// </summary>
[AIToolSource(typeof(PlantDataService))]
public partial class PlantManagementTools : AIToolContext { }

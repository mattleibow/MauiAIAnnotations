using AnnotationsSampleApp.Services;
using Microsoft.Extensions.AI.Attributes;

namespace AnnotationsSampleApp;

// ── Tool Contexts ────────────────────────────────────────────────
//
// Each context declares which service types contribute AI tools.
// The source generator scans [ExportAIFunction] methods at compile
// time and emits registration code — no runtime reflection needed.
//
// Contexts can overlap: the same service can appear in multiple contexts.

/// <summary>
/// All garden tools: catalog lookups, garden management, and weather advice.
/// Registered as the default (non-keyed) tool set.
/// </summary>
[AIToolSource(typeof(PlantCatalogService))]
[AIToolSource(typeof(GardenService))]
[AIToolSource(typeof(WeatherService))]
public partial class AllGardenTools : AIToolContext { }

/// <summary>
/// Read-only catalog tools: plant lookups and weather advice only.
/// No mutations possible — safe for a "browse" mode.
/// </summary>
[AIToolSource(typeof(PlantCatalogService))]
[AIToolSource(typeof(WeatherService))]
public partial class CatalogTools : AIToolContext { }

/// <summary>
/// Garden management tools: add, remove, water, and list plants.
/// A focused subset for plant management only.
/// </summary>
[AIToolSource(typeof(GardenService))]
public partial class GardenManagementTools : AIToolContext { }

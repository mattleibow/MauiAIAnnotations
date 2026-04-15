# Microsoft.Extensions.AI.Attributes

An attribute-driven AI tool discovery library for .NET 10. Decorate your service methods with `[ExportAIFunction]` and call `AddAITools()` to automatically register them as AI tools via `Microsoft.Extensions.AI`.

```csharp
[Description("Gets all plants the user has registered.")]
[ExportAIFunction("get_plants")]
public async Task<List<Plant>> GetPlantsAsync() { ... }
```

```csharp
builder.Services.AddAITools(typeof(PlantDataService).Assembly);
```

For full documentation, guides, and examples, see the [docs](../../docs/README.md).

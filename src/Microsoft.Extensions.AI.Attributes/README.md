# Microsoft.Extensions.AI.Attributes

Source-generated AI tool discovery for .NET 10. Decorate service methods with `[ExportAIFunction]`, group them into tool contexts with `[AIToolSource]`, and register everything in DI — no runtime reflection needed.

## How It Works

### 1. Annotate your service methods

```csharp
using System.ComponentModel;
using Microsoft.Extensions.AI.Attributes;

public class PlantCatalogService
{
    [Description("Searches the plant catalog by name or category.")]
    [ExportAIFunction("search_plants")]
    public List<PlantInfo> SearchPlants(
        [Description("Optional filter text")] string? query = null)
    {
        // ...
    }
}
```

- `[ExportAIFunction]` marks a method as an AI-callable tool
- `[ExportAIFunction("custom_name")]` overrides the tool name (defaults to method name)
- `[Description]` on the method and parameters provides AI-visible documentation
- `ApprovalRequired = true` wraps the tool so it requires user approval before execution

### 2. Define a tool context

```csharp
using Microsoft.Extensions.AI.Attributes;

[AIToolSource(typeof(PlantCatalogService))]
[AIToolSource(typeof(GardenService))]
public partial class AllGardenTools : AIToolContext { }
```

The **source generator** scans each `[AIToolSource]` type for `[ExportAIFunction]` methods at compile time and emits the registration code. No reflection, AOT-safe.

Multiple contexts can overlap — the same service can appear in several contexts:

```csharp
[AIToolSource(typeof(PlantCatalogService))]
public partial class CatalogTools : AIToolContext { }

[AIToolSource(typeof(GardenService))]
public partial class GardenManagementTools : AIToolContext { }
```

### 3. Register tools in DI

```csharp
// Default (non-keyed) — tools go into IEnumerable<AITool>
builder.Services.AddAITools<AllGardenTools>();

// Keyed — tools registered under a service key
builder.Services.AddAITools<GardenManagementTools>("management");

// Hand-crafted tools coexist with generated tools
builder.Services.AddSingleton<AITool>(
    AIFunctionFactory.Create(() => DateTime.Now.ToString("f"),
        "get_current_datetime", "Gets the current date and time."));
```

### 4. Use tools on demand (no DI registration)

```csharp
var tools = CatalogTools.Default.GetTools(serviceProvider);
// Use with any IChatClient directly
```

### 5. Inject tools into an IChatClient pipeline

```csharp
// Auto-inject all tools from DI
var client = chatClient.AsBuilder()
    .UseFunctionInvocation()
    .UseTools()
    .Build(serviceProvider);

// Or inject a specific tool context
var tools = CatalogTools.Default.GetTools(serviceProvider);
var client = chatClient.AsBuilder()
    .UseFunctionInvocation()
    .UseTools(tools)
    .Build(serviceProvider);

// No need to pass ChatOptions.Tools on each request — just call:
await foreach (var update in client.GetStreamingResponseAsync(messages))
{
    Console.Write(update.Text);
}
```

## Service Lifetimes

Tools are DI singletons, but the **underlying service** is resolved per-invocation via `DependencyInjectionAIFunction`. Register your services with the lifetime that fits:

| Lifetime | Behavior | Example |
|---|---|---|
| Singleton | Shared across all sessions | Static catalog data |
| Scoped | One instance per DI scope | Per-session state |
| Transient | New instance per tool call | Stateless utilities |

## Project References

This library ships as two projects:

```xml
<!-- The attributes and base types -->
<ProjectReference Include="Microsoft.Extensions.AI.Attributes.csproj" />

<!-- The source generator (analyzer, not a runtime reference) -->
<ProjectReference Include="Microsoft.Extensions.AI.Attributes.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

## Key Types

| Type | Description |
|---|---|
| `ExportAIFunctionAttribute` | Marks a method as an AI tool |
| `AIToolSourceAttribute` | Declares which service contributes tools to a context |
| `AIToolContext` | Base class for source-generated tool contexts |
| `AddAITools<T>()` | Extension method to register tools from a context |
| `UseTools()` | Injects tools into an `IChatClient` pipeline |
| `DependencyInjectionAIFunction` | Internal — resolves service from DI per invocation |

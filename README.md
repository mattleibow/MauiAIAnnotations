# Microsoft.Extensions.AI Attributes / Chat / Maui

Turn regular .NET services into AI-callable tools — no hand-written JSON schemas, tool adapters, or runtime reflection.

> **Status:** Work in progress. The Chat and Maui libraries are not yet ready for use.

## Quick start: Annotations

### 1. Annotate your service methods

```csharp
using Microsoft.Extensions.AI.Attributes;
using System.ComponentModel;

public class PlantDataService
{
    [Description("Gets all plants the user has registered.")]
    [ExportAIFunction("get_plants")]
    public async Task<List<Plant>> GetPlantsAsync() => ...;

    [Description("Adds a new plant.")]
    [ExportAIFunction("add_plant", ApprovalRequired = true)]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name")] string nickname,
        [Description("The species")] string species) => ...;
}
```

### 2. Define a tool context

```csharp
[AIToolSource(typeof(PlantDataService))]
[AIToolSource(typeof(SeasonsService))]
public partial class GardenTools : AIToolContext { }
```

The source generator emits the registration code at compile time — no reflection.

### 3. Register in DI

```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools<GardenTools>();
```

### 4. Use with any IChatClient

```csharp
var tools = serviceProvider.GetServices<AITool>();
var client = chatClient.AsBuilder()
    .UseFunctionInvocation()
    .Build(serviceProvider);

var options = new ChatOptions { Tools = [.. tools] };
await foreach (var update in client.GetStreamingResponseAsync(messages, options))
{
    Console.Write(update.Text);
}
```

## Libraries

| Library | Status | README |
| --- | --- | --- |
| [Microsoft.Extensions.AI.Attributes](src/Microsoft.Extensions.AI.Attributes/) | ✅ Ready | [README](src/Microsoft.Extensions.AI.Attributes/README.md) |
| [Microsoft.Extensions.AI.Chat](src/Microsoft.Extensions.AI.Chat/) | 🚧 WIP | [README](src/Microsoft.Extensions.AI.Chat/README.md) |
| [Microsoft.Extensions.AI.Maui](src/Microsoft.Extensions.AI.Maui/) | 🚧 WIP | [README](src/Microsoft.Extensions.AI.Maui/README.md) |

## Sample apps

| App | Description |
| --- | --- |
| `AnnotationsSampleApp` | Focused demo of annotations, tool contexts, and DI lifetimes |
| `MauiSampleApp` | Full garden helper with chat UI, approval views, and DevFlow |

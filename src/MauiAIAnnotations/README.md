# MauiAIAnnotations

An attribute-driven AI tool discovery library for .NET. Decorate your service methods with `[ExportAIFunction]` and let the library automatically expose them as AI tools via `Microsoft.Extensions.AI`.

## Quick Start

### 1. Annotate your service methods

```csharp
using MauiAIAnnotations;
using System.ComponentModel;

public class PlantDataService
{
    [ExportAIFunction("get_plants", Description = "Gets all plants the user has registered.")]
    public async Task<List<Plant>> GetPlantsAsync() { ... }

    [ExportAIFunction("add_plant", Description = "Adds a new plant to the garden.")]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name for the plant")] string nickname,
        [Description("The species name")] string species,
        [Description("Where the plant is located")] string location,
        [Description("Whether the plant is kept indoors")] bool isIndoor) { ... }
}
```

### 2. Register in DI

```csharp
// Register your services as usual
builder.Services.AddSingleton<PlantDataService>();

// Scan an assembly for [ExportAIFunction] methods
builder.Services.AddAITools(typeof(PlantDataService).Assembly);
```

### 3. Use with IChatClient

```csharp
public class ChatViewModel
{
    private readonly IList<AITool> _tools;
    private readonly IChatClient _chatClient;

    public ChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
    {
        _tools = tools.ToList();
        _chatClient = chatClient;
    }

    async Task SendAsync(string message)
    {
        var options = new ChatOptions { Tools = _tools };
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options))
        {
            // handle streaming response
        }
    }
}
```

## API Reference

### ExportAIFunctionAttribute

Marks a public instance method to be exported as an AI tool.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | Tool name exposed to the AI model. Defaults to the method name. |
| `Description` | `string?` | Description for the AI model. Falls back to `[Description]` on the method. |

### ServiceCollectionExtensions

| Method | Description |
|--------|-------------|
| `AddAITools()` | Scans the calling assembly for annotated types. |
| `AddAITools(params Assembly[])` | Scans specified assemblies. |
| `AddAITools(params Type[])` | Registers specific types to scan. |

Each discovered method is registered as a singleton `AITool` in DI. Consumers inject `IEnumerable<AITool>` to receive all tools.

## How It Works

1. **Discovery**: `ReflectionAIToolProvider` scans registered types for methods with `[ExportAIFunction]`.
2. **Schema**: Each discovered method is wrapped in a `DependencyInjectionAIFunction` that exposes the correct JSON schema (parameter names, types, descriptions) to the AI model.
3. **Invocation**: When the AI model calls a tool, the service instance is resolved from DI on each invocation, respecting DI lifetimes (singleton, transient, scoped).
4. **No disposal**: The library does NOT dispose services after invocation — DI container manages service lifetimes.

## Requirements

- .NET 10.0+
- `Microsoft.Extensions.AI` 10.4.1+
- `Microsoft.Extensions.DependencyInjection` 10.0.0+

## Important: ChatClientBuilder.Build(provider)

When setting up your `IChatClient` pipeline with `UseFunctionInvocation()`, pass the `IServiceProvider` to `.Build(provider)` so that `AIFunctionArguments.Services` is populated correctly:

```csharp
builder.Services.AddSingleton<IChatClient>(provider =>
{
    return chatClient.AsIChatClient()
        .AsBuilder()
        .UseLogging(lf)
        .UseFunctionInvocation()
        .Build(provider);  // Pass the service provider
});
```

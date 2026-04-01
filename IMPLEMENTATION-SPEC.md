# Implementation Spec: Attribute-Based AI Tool Discovery

> **Purpose:** This document is a complete, self-contained prompt for an AI agent to implement the attribute-based AI tool discovery system for MauiAIAnnotations. It contains all context, design decisions, file-by-file changes, and expected outcomes needed to execute the work without additional human input.

---

## Context & Goal

The `MauiAIAnnotations` library (currently an empty project at `src/MauiAIAnnotations/`) needs to provide an attribute-driven system that lets developers expose service methods as AI tools without manual wiring code.

**Today**, the sample app's `ChatViewModel` has ~70 lines of boilerplate in `SetupAIFunctions()` that manually creates 7 `AIFunction` wrappers using `AIFunctionFactory.Create(lambda, name, description)`. Each lambda calls a service method, serializes the result, and returns a string. This is brittle and doesn't scale.

**After this work**, developers will:
1. Put `[ExportAIFunction]` on service methods
2. Use `[Description]` on parameters (standard `System.ComponentModel`)
3. Call `builder.Services.AddAIToolProvider()` in DI setup
4. Inject `IAIToolProvider` wherever tools are needed

The `ChatViewModel` will go from ~130 lines to ~60 lines with zero manual tool setup.

---

## Repository Structure

```
MauiAIAnnotations.slnx
src/
  MauiAIAnnotations/                    ← Library (currently empty .csproj)
    MauiAIAnnotations.csproj            ← Exists, targets net10.0
    README.md                           ← TO CREATE: library documentation
    ExportAIFunctionAttribute.cs        ← TO CREATE
    IAIToolProvider.cs                  ← TO CREATE
    ReflectionAIToolProvider.cs         ← TO CREATE
    ServiceCollectionExtensions.cs      ← TO CREATE
samples/
  MauiSampleApp/                        ← MAUI app (head project)
    MauiProgram.cs                      ← TO MODIFY: add AddAIToolProvider()
    ViewModels/
      ChatViewModel.cs                  ← TO MODIFY: simplify drastically
  MauiSampleApp.Core/                   ← Shared services & models
    MauiSampleApp.Core.csproj           ← TO MODIFY: add MauiAIAnnotations ref
    Services/
      PlantDataService.cs               ← TO MODIFY: add attributes
      SpeciesService.cs                 ← TO MODIFY: add attributes
    Models/
      GardenModels.cs                   ← NO CHANGES
tests/
  MauiAIAnnotations.Tests/             ← TO ADD: new test files
  MauiSampleApp.Core.Tests/
    GardenServiceTests.cs              ← VERIFY: still passes
```

---

## Phase 1: MauiAIAnnotations Library

### 1.1 Update `src/MauiAIAnnotations/MauiAIAnnotations.csproj`

The current file is:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

Add package references. The final file should be:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="10.4.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
  </ItemGroup>
</Project>
```

**Important:** Only reference `Abstractions` packages. Do NOT reference `Microsoft.Extensions.AI` (the full package) or any Azure/OpenAI packages. The version of `Microsoft.Extensions.AI.Abstractions` should match what `MauiSampleApp.Core.csproj` uses (10.4.1).

### 1.2 Create `src/MauiAIAnnotations/ExportAIFunctionAttribute.cs`

```csharp
using System;

namespace MauiAIAnnotations;

/// <summary>
/// Marks a method to be exported as an AI tool function.
/// Methods with this attribute are discovered by <see cref="IAIToolProvider"/>
/// and made available to AI chat clients as callable tools.
/// </summary>
/// <remarks>
/// The method must be a public instance method on a type that is registered in DI.
/// Parameter descriptions should use <see cref="System.ComponentModel.DescriptionAttribute"/>.
/// Return values are automatically serialized by Microsoft.Extensions.AI.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ExportAIFunctionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance with default name (derived from method name).
    /// </summary>
    public ExportAIFunctionAttribute() { }

    /// <summary>
    /// Initializes a new instance with an explicit tool name.
    /// </summary>
    /// <param name="name">The name of the tool as exposed to the AI model.</param>
    public ExportAIFunctionAttribute(string name) => Name = name;

    /// <summary>
    /// The tool name exposed to the AI model (e.g. "get_plants").
    /// If not set, defaults to the method name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description shown to the AI model explaining what this tool does.
    /// If not set, falls back to <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// on the method, if present.
    /// </summary>
    public string? Description { get; set; }
}
```

### 1.3 Create `src/MauiAIAnnotations/IAIToolProvider.cs`

```csharp
using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Provides a collection of AI tools that can be used with an <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// Implementations discover and create <see cref="AITool"/> instances from
/// service methods annotated with <see cref="ExportAIFunctionAttribute"/>.
/// Consumers set <c>ChatOptions.Tools</c> to the result of <see cref="GetTools"/>.
/// </remarks>
public interface IAIToolProvider
{
    /// <summary>
    /// Gets the collection of AI tools available for use.
    /// </summary>
    /// <returns>A read-only list of AI tools.</returns>
    IReadOnlyList<AITool> GetTools();
}
```

### 1.4 Create `src/MauiAIAnnotations/ReflectionAIToolProvider.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// An <see cref="IAIToolProvider"/> that discovers AI tools via reflection,
/// scanning for methods annotated with <see cref="ExportAIFunctionAttribute"/>.
/// </summary>
/// <remarks>
/// Service instances are resolved from the <see cref="IServiceProvider"/> on first
/// call to <see cref="GetTools"/>. The tool list is cached for the lifetime of the provider.
/// </remarks>
public class ReflectionAIToolProvider : IAIToolProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<ToolRegistration> _registrations;
    private IReadOnlyList<AITool>? _tools;

    /// <summary>
    /// Creates a new provider that will scan the given types for annotated methods.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider used to resolve service instances.</param>
    /// <param name="serviceTypes">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    public ReflectionAIToolProvider(IServiceProvider serviceProvider, IEnumerable<Type> serviceTypes)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registrations = DiscoverRegistrations(serviceTypes).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> GetTools()
    {
        return _tools ??= BuildTools();
    }

    private IReadOnlyList<AITool> BuildTools()
    {
        var tools = new List<AITool>();

        foreach (var reg in _registrations)
        {
            var instance = _serviceProvider.GetService(reg.ServiceType)
                ?? throw new InvalidOperationException(
                    $"Service type '{reg.ServiceType.FullName}' is not registered in DI but has " +
                    $"[ExportAIFunction] on method '{reg.Method.Name}'. Register it in the service collection.");

            var options = new AIFunctionFactoryOptions
            {
                Name = reg.Name,
                Description = reg.Description,
            };

            tools.Add(AIFunctionFactory.Create(reg.Method, instance, options));
        }

        return tools.AsReadOnly();
    }

    private static IEnumerable<ToolRegistration> DiscoverRegistrations(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ExportAIFunctionAttribute>();
                if (attr is null)
                    continue;

                var name = attr.Name ?? method.Name;
                var description = attr.Description
                    ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description;

                yield return new ToolRegistration(type, method, name, description);
            }
        }
    }

    internal sealed record ToolRegistration(
        Type ServiceType,
        MethodInfo Method,
        string Name,
        string? Description);
}
```

### 1.5 Create `src/MauiAIAnnotations/ServiceCollectionExtensions.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MauiAIAnnotations;

/// <summary>
/// Extension methods for registering AI tool providers in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the calling assembly for types containing methods annotated with
    /// <see cref="ExportAIFunctionAttribute"/> and registers an <see cref="IAIToolProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIToolProvider(
        this IServiceCollection services,
        [CallerMemberName] string? _ = null)
    {
        return services.AddAIToolProvider(Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Scans the specified assemblies for types containing methods annotated with
    /// <see cref="ExportAIFunctionAttribute"/> and registers an <see cref="IAIToolProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIToolProvider(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        var types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(TypeHasExportedFunctions)
            .ToList();

        return services.AddAIToolProvider(types.ToArray());
    }

    /// <summary>
    /// Registers an <see cref="IAIToolProvider"/> that provides AI tools from the
    /// specified types' annotated methods.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="types">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIToolProvider(
        this IServiceCollection services,
        params Type[] types)
    {
        // Capture the types list for the factory closure
        var serviceTypes = types.ToList();

        services.TryAddSingleton<IAIToolProvider>(sp =>
            new ReflectionAIToolProvider(sp, serviceTypes));

        return services;
    }

    private static bool TypeHasExportedFunctions(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.GetCustomAttribute<ExportAIFunctionAttribute>() is not null);
    }
}
```

### 1.6 Create `src/MauiAIAnnotations/README.md`

Create a comprehensive README for the library. See the full content in the "Library README" section below.

---

## Phase 2: Annotate Sample Services

### 2.1 Update `samples/MauiSampleApp.Core/MauiSampleApp.Core.csproj`

Add a project reference to MauiAIAnnotations:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="10.4.1" />
  <PackageReference Include="Microsoft.Extensions.AI.Abstractions" Version="10.4.1" />
  <PackageReference Include="Shiny.DocumentDb.Sqlite" Version="3.2.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\..\src\MauiAIAnnotations\MauiAIAnnotations.csproj" />
</ItemGroup>
```

### 2.2 Modify `samples/MauiSampleApp.Core/Services/PlantDataService.cs`

Add `using MauiAIAnnotations;` and `using System.ComponentModel;` at the top.

Add attributes to these 6 methods (do NOT change method signatures or bodies):

```csharp
[ExportAIFunction("get_plants", Description = "Gets all plants the user has registered.")]
public async Task<List<Plant>> GetPlantsAsync()

[ExportAIFunction("get_plant", Description = "Gets a specific plant by its nickname.")]
public async Task<Plant?> GetPlantAsync(
    [Description("The nickname of the plant to look up")] string nickname)

[ExportAIFunction("add_plant", Description = "Adds a new plant. Requires nickname, species name, location, and whether it's indoors.")]
public async Task<Plant> AddPlantAsync(
    [Description("A friendly name for the plant")] string nickname,
    [Description("The species common name (e.g. 'tomato', 'basil')")] string species,
    [Description("Where the plant is located")] string location,
    [Description("Whether the plant is indoors")] bool isIndoor)

[ExportAIFunction("remove_plant", Description = "Removes a plant by its nickname.")]
public async Task RemovePlantAsync(
    [Description("The nickname of the plant to remove")] string nickname)

[ExportAIFunction("log_care_event", Description = "Logs a care event for a plant. EventType must be one of: Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed.")]
public async Task<CareEvent> LogCareEventAsync(
    [Description("The nickname of the plant")] string plantNickname,
    [Description("One of: Watered, Fertilized, Pruned, Repotted, TreatedForPest, Observed")] string eventType,
    [Description("Additional notes about the care event")] string notes)

[ExportAIFunction("get_care_history", Description = "Gets the care history for a plant by its nickname.")]
public async Task<List<CareEvent>> GetCareHistoryAsync(
    [Description("The nickname of the plant")] string plantNickname)
```

### 2.3 Modify `samples/MauiSampleApp.Core/Services/SpeciesService.cs`

Add `using MauiAIAnnotations;` and `using System.ComponentModel;` at the top.

Add attribute to `GetSpeciesAsync` only (NOT `GetSpeciesByIdAsync` — that's an internal lookup, not an AI tool):

```csharp
[ExportAIFunction("get_species", Description = "Gets a species profile by common name (e.g. 'tomato', 'basil'). Returns care information including watering frequency, sunlight needs, and frost tolerance.")]
public async Task<SpeciesProfile> GetSpeciesAsync(
    [Description("The common name of the plant species")] string name)
```

---

## Phase 3: Simplify ChatViewModel + DI Registration

### 3.1 Modify `samples/MauiSampleApp/ViewModels/ChatViewModel.cs`

Replace the entire file. The new version:

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiAIAnnotations;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.ViewModels;

public record ConversationEntry(string Role, string Content);

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly IChatClient? _chatClient;
    private readonly IAIToolProvider _toolProvider;
    private string _userInput = string.Empty;
    private bool _isBusy;

    private static readonly string SystemPrompt = """
        You are a friendly gardening assistant. You help users manage their plants, track care events, and answer plant care questions.
        You have access to tools to look up species information, manage plants, and log care events.
        When a user mentions adding a plant, use the AddPlant tool. When they ask about care, look up the species profile and care history.
        Be conversational and helpful. Use emoji occasionally to be friendly 🌱
        """;

    public ChatViewModel(IAIToolProvider toolProvider, IChatClient? chatClient = null)
    {
        _chatClient = chatClient;
        _toolProvider = toolProvider;
        Messages = [];
        SendCommand = new Command(async () => await SendMessageAsync(), () => !IsBusy);
    }

    public ObservableCollection<ConversationEntry> Messages { get; }

    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public ICommand SendCommand { get; }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput) || _chatClient is null)
        {
            if (_chatClient is null && !string.IsNullOrWhiteSpace(UserInput))
            {
                UserInput = string.Empty;
                Messages.Add(new ConversationEntry("Assistant", "AI is not configured. Please set up user secrets with AI:ApiKey, AI:Endpoint, and AI:DeploymentName."));
            }
            return;
        }

        var userMessage = UserInput;
        UserInput = string.Empty;
        Messages.Add(new ConversationEntry("User", userMessage));
        IsBusy = true;

        try
        {
            var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };

            foreach (var m in Messages)
            {
                history.Add(m.Role == "User"
                    ? new ChatMessage(ChatRole.User, m.Content)
                    : new ChatMessage(ChatRole.Assistant, m.Content));
            }

            var options = new ChatOptions { Tools = _toolProvider.GetTools() };

            // Streaming response
            var index = Messages.Count;
            Messages.Add(new ConversationEntry("Assistant", ""));
            var responseText = "";

            await foreach (var update in _chatClient.GetStreamingResponseAsync(history, options))
            {
                if (update.Text is { } text)
                {
                    responseText += text;
                    Messages[index] = new ConversationEntry("Assistant", responseText);
                }
            }

            if (string.IsNullOrEmpty(responseText))
                Messages[index] = new ConversationEntry("Assistant", "(no response)");
        }
        catch (Exception ex)
        {
            Messages.Add(new ConversationEntry("Assistant", $"Error: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

**Key changes:**
- Removed `using MauiSampleApp.Core.Services;` and `using System.Text.Json;`
- Added `using MauiAIAnnotations;`
- Removed `_speciesService` and `_plantDataService` fields
- Constructor takes `IAIToolProvider` instead of the two services
- Removed `SetupAIFunctions()` method entirely (~70 lines deleted)
- Removed `_aiTools` field
- `SendMessageAsync` uses `_toolProvider.GetTools()` instead of `_aiTools`

### 3.2 Modify `samples/MauiSampleApp/MauiProgram.cs`

Add `using MauiAIAnnotations;` at the top.

In `CreateMauiAppCore()`, after registering services but before registering ViewModels, add:

```csharp
// Register AI tool provider (discovers [ExportAIFunction] methods)
builder.Services.AddAIToolProvider(typeof(PlantDataService).Assembly);
```

The `ChatViewModel` registration stays as `AddSingleton<ChatViewModel>()` — DI will resolve `IAIToolProvider` and `IChatClient?` automatically.

---

## Phase 4: Tests

### 4.1 Add tests in `tests/MauiAIAnnotations.Tests/`

The test project already has a `.csproj`. Create a test file `ReflectionAIToolProviderTests.cs`:

Test cases to implement:
1. **Discovers methods with `[ExportAIFunction]`** — scan a test type, verify correct number of tools
2. **Uses custom Name from attribute** — verify the tool name matches the attribute's Name
3. **Falls back to method name** — when Name is not set, uses method name
4. **Uses Description from attribute** — verify description is passed through
5. **Falls back to `[Description]` attribute** — when Description not set on `[ExportAIFunction]`, uses `[DescriptionAttribute]`
6. **Ignores methods without attribute** — non-annotated methods are not tools
7. **Resolves service from DI** — mock/real service provider, verify service is resolved
8. **Throws when service not in DI** — type has attribute but isn't registered in DI
9. **Assembly scanning finds types** — `AddAIToolProvider(assembly)` discovers annotated types
10. **Explicit type scanning works** — `AddAIToolProvider(typeof(T))` discovers annotated methods

Create test helper types (in the test project):
```csharp
public class TestToolService
{
    [ExportAIFunction("test_tool", Description = "A test tool")]
    public string DoSomething([Description("input value")] string input) => $"result: {input}";

    [ExportAIFunction] // no name or description
    public int GetCount() => 42;

    // NOT a tool - no attribute
    public void InternalMethod() { }
}
```

### 4.2 Verify existing tests

Run `dotnet test tests/MauiSampleApp.Core.Tests/` and confirm all `GardenServiceTests` pass unchanged. The service method signatures and behavior are unchanged — only attributes were added.

---

## Library README Content

Create `src/MauiAIAnnotations/README.md` with the following content:

````markdown
# MauiAIAnnotations

Attribute-based AI tool discovery for .NET applications using [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI).

## Overview

MauiAIAnnotations eliminates boilerplate when exposing service methods as AI tools. Instead of manually creating `AIFunction` wrappers, annotate your methods and let the library discover and wire them automatically.

### Before (manual wiring)

```csharp
// 70+ lines of manual tool setup in your ViewModel
var getPlants = AIFunctionFactory.Create(
    async () =>
    {
        var plants = await _plantDataService.GetPlantsAsync();
        return JsonSerializer.Serialize(plants);
    },
    "get_plants",
    "Gets all plants the user has registered.");

var addPlant = AIFunctionFactory.Create(
    async (string nickname, string species, string location, bool isIndoor) =>
    {
        var plant = await _plantDataService.AddPlantAsync(nickname, species, location, isIndoor);
        return $"Added plant '{plant.Nickname}'.";
    },
    "add_plant",
    "Adds a new plant.");
// ... repeat for every tool
```

### After (attribute-based)

```csharp
// On your service (the only code you write):
public class PlantDataService
{
    [ExportAIFunction("get_plants", Description = "Gets all plants the user has registered.")]
    public async Task<List<Plant>> GetPlantsAsync() { /* ... */ }

    [ExportAIFunction("add_plant", Description = "Adds a new plant.")]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name")] string nickname,
        [Description("The species name")] string species,
        [Description("Where the plant is")] string location,
        [Description("Whether indoors")] bool isIndoor) { /* ... */ }
}

// In your ViewModel (one line):
var options = new ChatOptions { Tools = _toolProvider.GetTools() };
```

## Installation

Add a reference to the `MauiAIAnnotations` project (or NuGet package when published):

```xml
<ProjectReference Include="path/to/MauiAIAnnotations.csproj" />
```

## Usage

### 1. Annotate your service methods

Use `[ExportAIFunction]` to mark methods as AI tools:

```csharp
using MauiAIAnnotations;
using System.ComponentModel;

public class WeatherService
{
    [ExportAIFunction("get_weather", Description = "Gets the current weather for a city.")]
    public async Task<WeatherData> GetWeatherAsync(
        [Description("The city name")] string city,
        [Description("Temperature unit: Celsius or Fahrenheit")] string unit = "Celsius")
    {
        // Your implementation
    }
}
```

**Attribute properties:**
- `Name` (optional) — The tool name exposed to the AI. Defaults to the method name.
- `Description` (optional) — Describes what the tool does. Falls back to `[Description]` on the method.

**Parameter descriptions:** Use the standard `[Description]` attribute from `System.ComponentModel`.

### 2. Register the tool provider

In your DI setup (`MauiProgram.cs`, `Program.cs`, etc.):

```csharp
using MauiAIAnnotations;

// Scan specific assemblies:
builder.Services.AddAIToolProvider(typeof(WeatherService).Assembly);

// Or scan specific types:
builder.Services.AddAIToolProvider(typeof(WeatherService), typeof(PlantService));
```

This registers an `IAIToolProvider` singleton that discovers all annotated methods.

### 3. Use the tool provider

Inject `IAIToolProvider` wherever you need AI tools:

```csharp
public class ChatViewModel
{
    private readonly IAIToolProvider _toolProvider;
    private readonly IChatClient _chatClient;

    public ChatViewModel(IAIToolProvider toolProvider, IChatClient chatClient)
    {
        _toolProvider = toolProvider;
        _chatClient = chatClient;
    }

    public async Task SendMessageAsync(string message)
    {
        var options = new ChatOptions { Tools = _toolProvider.GetTools() };
        var response = await _chatClient.GetResponseAsync(message, options);
    }
}
```

## How It Works

1. **Discovery:** At registration time, `AddAIToolProvider()` scans types/assemblies for public instance methods with `[ExportAIFunction]`.
2. **Registration:** A `ReflectionAIToolProvider` is registered as a singleton `IAIToolProvider`.
3. **Tool creation:** On first `GetTools()` call, service instances are resolved from DI and `AIFunctionFactory.Create(MethodInfo, instance)` builds the `AITool` list.
4. **Caching:** The tool list is cached — subsequent `GetTools()` calls return the same list.

### Service lifetime

Services are resolved from DI with their registered lifetime (singleton, scoped, transient). The provider does not create extra instances.

### Return value handling

`Microsoft.Extensions.AI` automatically serializes return values to JSON for the AI model. You do NOT need to manually serialize — just return your typed objects.

## API Reference

### `ExportAIFunctionAttribute`

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ExportAIFunctionAttribute : Attribute
{
    public ExportAIFunctionAttribute();
    public ExportAIFunctionAttribute(string name);
    public string? Name { get; set; }
    public string? Description { get; set; }
}
```

### `IAIToolProvider`

```csharp
public interface IAIToolProvider
{
    IReadOnlyList<AITool> GetTools();
}
```

### Extension Methods

```csharp
public static class ServiceCollectionExtensions
{
    // Scan calling assembly
    public static IServiceCollection AddAIToolProvider(this IServiceCollection services);

    // Scan specific assemblies
    public static IServiceCollection AddAIToolProvider(this IServiceCollection services, params Assembly[] assemblies);

    // Scan specific types
    public static IServiceCollection AddAIToolProvider(this IServiceCollection services, params Type[] types);
}
```

## Design Principles

- **Minimal ceremony:** One attribute per method, one registration call.
- **Standards-based:** Uses `[Description]` from `System.ComponentModel` for parameter docs (same as M.E.AI).
- **DI-native:** Services are resolved from the container; lifetimes are respected.
- **Source-gen ready:** The attribute + interface contract is stable. A future source generator can replace the reflection implementation without changing user code.
- **No hard dependencies:** Only depends on `Microsoft.Extensions.AI.Abstractions` and `Microsoft.Extensions.DependencyInjection.Abstractions`.

## Future: Source Generator

A planned source generator will:
1. Scan for `[ExportAIFunction]` at compile time
2. Generate a concrete `IAIToolProvider` with no reflection
3. Generate an extension method like `builder.RegisterAiServices()`
4. Enable AOT/trimming compatibility

The attribute and `IAIToolProvider` interface will remain unchanged.
````

---

## Verification Checklist

After implementation, verify:

- [ ] `dotnet build src/MauiAIAnnotations/` succeeds
- [ ] `dotnet build samples/MauiSampleApp.Core/` succeeds (has MauiAIAnnotations ref + attributes)
- [ ] `dotnet build samples/MauiSampleApp/` succeeds (ChatViewModel uses IAIToolProvider)
- [ ] `dotnet test tests/MauiSampleApp.Core.Tests/` — all existing tests pass
- [ ] `dotnet test tests/MauiAIAnnotations.Tests/` — new tests pass
- [ ] `ChatViewModel` no longer directly references `PlantDataService` or `SpeciesService`
- [ ] `ChatViewModel` has no `SetupAIFunctions` method
- [ ] `PlantDataService` has 6 methods with `[ExportAIFunction]`
- [ ] `SpeciesService` has 1 method with `[ExportAIFunction]`
- [ ] `src/MauiAIAnnotations/README.md` exists with full documentation

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Attribute on **methods** not classes | Not all methods should be tools; explicit opt-in per method |
| `IAIToolProvider` as DI interface | Clean separation; consumers don't know about discovery mechanism |
| Lazy tool creation in `GetTools()` | Services must be available from DI before tools can be built |
| Return raw objects, not strings | M.E.AI serializes automatically; structured data is better for AI |
| `[Description]` for parameters | Standard .NET attribute; M.E.AI already reads it natively |
| Assembly scanning by default | Minimizes registration boilerplate |
| Singleton `IAIToolProvider` | Tool list is stable; no need to rebuild per request |
| Only Abstractions dependencies | Library stays lightweight; no Azure/OpenAI coupling |

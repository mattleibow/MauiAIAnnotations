# Implementation Spec: Attribute-Based AI Tool Discovery

> **Purpose:** This document is a complete, self-contained prompt for an AI agent to implement the attribute-based AI tool discovery system for MauiAIAnnotations. It contains all context, design decisions, file-by-file changes, and expected outcomes needed to execute the work without additional human input.

## Instructions for the Implementing Agent

> **READ THIS FIRST. This spec has been through multiple review rounds but is NOT guaranteed to be correct.**

### Mindset: Trust Nothing, Validate Everything

This spec was developed iteratively with multi-model review (Claude Opus 4.5/4.6, GPT 5.3/5.4). Each round found critical issues — wrong API names, missing overrides, incorrect DI assumptions, disposal hazards. The current version addresses all known issues, but **you must treat every claim as a hypothesis to be validated, not a fact to be copied.**

### Implementation Process

1. **Implement incrementally.** Do ONE file/phase at a time. Build and verify after each step. Do not batch-implement the entire spec and hope it compiles.

2. **Validate every API claim against the actual packages.** This spec references `Microsoft.Extensions.AI.Abstractions` 10.4.1 APIs (`AIFunction`, `AIFunctionFactory`, `AIFunctionDeclaration`, `AIFunctionFactoryOptions`, `AIFunctionArguments`, `ChatClientBuilder`). Before writing code that uses these types, verify the actual method signatures, property types, and inheritance chain in the installed package. The spec may still have errors — prior rounds found that `CreateDeclaration(MethodInfo)` didn't exist, `Description` had wrong nullability, and `JsonSchema`/`ReturnJsonSchema` overrides were missing. More may remain.

3. **Run a multi-agent code review after each phase.** After completing each phase (library, annotations, ChatViewModel, tests), request a review from at least two different models. Reviewers should verify compilation, API correctness, DI lifetime behavior, and test coverage.

4. **Test aggressively.** The test section defines a minimum set of cases. During implementation, you WILL discover edge cases, unexpected behaviors, or API quirks not covered by this spec. **Write a test for every issue you find.** The goal is to grow the test base so that no bug occurs twice. If you fix something, prove it's fixed with a test. If you discover a limitation, document it with a test that asserts the expected behavior (or expected failure).

5. **The sample app is the proof.** The library is only useful if the sample app works end-to-end. After all phases, the sample app must build, the chat must work with AI tools, and all 7 original tools must function identically to before (get_species, get_plants, get_plant, add_plant, remove_plant, log_care_event, get_care_history). Verify by comparing behavior, not just compilation.

6. **DI lifetimes are the hardest part.** The `DependencyInjectionAIFunction` class is the core of the design and the most likely place for subtle bugs. Test all lifetime combinations (singleton, transient, scoped) × provider types (root, scoped, empty) with `ValidateScopes = true`. Do not skip these tests — they are the entire reason this custom subclass exists instead of using `AIFunctionFactory` directly.

7. **When in doubt, investigate.** If something doesn't compile, doesn't work, or feels wrong — stop and investigate. Read the actual M.E.AI source code. Check the NuGet package XML docs. Search dotnet/extensions issues. Do not guess or work around problems blindly.

### Known Risk Areas (from review rounds)

- **`AIFunctionFactory` overloads**: The `Func<AIFunctionArguments, object>` overload auto-disposes returned instances. The `(MethodInfo, object instance)` overload does NOT. This distinction is critical and must be verified.
- **`ChatClientBuilder.Build()` vs `.Build(provider)`**: Without passing the `IServiceProvider`, `args.Services` is set to an internal `EmptyServiceProvider` — non-null but useless. The sample app MUST use `.Build(provider)`.
- **Schema propagation**: `DependencyInjectionAIFunction` must override `JsonSchema` and `ReturnJsonSchema` (not just `Name`/`Description`), or the AI model gets empty parameter schemas.
- **`ResolveService` fallback**: The single-resolve pattern avoids double-instantiation of transient services but still calls `GetService` on the provided provider before potentially falling back to root. Verify this doesn't cause issues with `ValidateScopes`.

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
    DependencyInjectionAIFunction.cs    ← TO CREATE: custom AIFunction subclass
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

### 1.4 Create `src/MauiAIAnnotations/DependencyInjectionAIFunction.cs`

This is a custom `AIFunction` subclass that resolves services from DI per invocation **without** disposing them (DI owns the lifetime).

**Why not use `AIFunctionFactory.Create(MethodInfo, Func<AIFunctionArguments, object>)`?**
That overload auto-disposes the returned instance after each invocation. For DI-owned services (singletons, scoped), this would prematurely destroy the service. The instance-based overload `Create(MethodInfo, object instance)` does NOT dispose, so we use that per-invocation for parameter binding.

**Key design decisions:**
- A "schema source" `AIFunction` is created once at construction time (via the `Func<>` overload with a dummy factory). It is **never invoked** — only used to read `JsonSchema`, `ReturnJsonSchema`, etc. This avoids the non-existent `CreateDeclaration(MethodInfo, options)` API and gives us correct metadata.
- Per invocation: resolve from DI → create a temp instance-bound `AIFunction` → delegate invocation. The temp is lightweight since `AIFunctionFactory` caches reflection descriptors internally.
- Service provider fallback handles the `EmptyServiceProvider` case: `ChatClientBuilder.Build()` without args sets `args.Services` to an internal empty provider (not null), so a simple `?? root` check doesn't work. The `ResolveService` method does a single resolution attempt from the provided provider, falling back to root only if it fails — avoiding the double-resolve problem of the earlier `ResolveServiceProvider` design.

```csharp
using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MauiAIAnnotations;

/// <summary>
/// A custom <see cref="AIFunction"/> that resolves a service from DI on each invocation
/// and delegates to <see cref="AIFunctionFactory"/> for parameter binding and marshaling,
/// without disposing the DI-owned service instance.
/// </summary>
/// <remarks>
/// <para>
/// <c>AIFunctionFactory.Create(MethodInfo, Func&lt;AIFunctionArguments, object&gt;)</c> auto-disposes
/// the returned instance after each call, which is incorrect for container-managed services.
/// This class avoids that by resolving from DI, then creating a temporary instance-bound
/// <see cref="AIFunction"/> via <c>AIFunctionFactory.Create(MethodInfo, object instance)</c> —
/// which does NOT dispose the target.
/// </para>
/// <para>
/// DI lifetimes are respected correctly:
/// <list type="bullet">
///   <item><b>Singleton</b> — DI returns the same instance every time.</item>
///   <item><b>Transient</b> — DI creates a fresh instance per tool invocation.</item>
///   <item><b>Scoped</b> — Resolves from the scoped <c>IServiceProvider</c> in
///     <c>AIFunctionArguments.Services</c> (set by <c>FunctionInvokingChatClient</c>).
///     Falls back to the root provider when the provided service provider cannot resolve
///     the service (e.g. when <c>ChatClientBuilder.Build()</c> was called without passing
///     a real provider, which sets <c>args.Services</c> to an internal empty provider).
///     With <c>ValidateScopes=true</c>, resolving scoped from root correctly throws.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DependencyInjectionAIFunction : AIFunction
{
    private readonly MethodInfo _method;
    private readonly Type _serviceType;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly AIFunctionFactoryOptions _factoryOptions;

    // Schema source: a factory-created AIFunction used ONLY for metadata (Name, Description,
    // JsonSchema, ReturnJsonSchema). Never invoked. Created once at construction time.
    private readonly AIFunction _schemaSource;

    public DependencyInjectionAIFunction(
        MethodInfo method,
        Type serviceType,
        IServiceProvider rootServiceProvider,
        string name,
        string? description)
    {
        _method = method ?? throw new ArgumentNullException(nameof(method));
        _serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        _rootServiceProvider = rootServiceProvider ?? throw new ArgumentNullException(nameof(rootServiceProvider));

        _factoryOptions = new AIFunctionFactoryOptions
        {
            Name = name,
            Description = description,
        };

        // Create a schema source AIFunction with a dummy factory. This computes the full
        // JSON schema, parameter metadata, return schema, etc. from the MethodInfo.
        // The dummy factory throws if somehow invoked — this AIFunction is metadata-only.
        _schemaSource = AIFunctionFactory.Create(
            _method,
            static _ => throw new InvalidOperationException("Schema source should not be invoked."),
            _factoryOptions);
    }

    // Delegate all metadata to the schema source (computed once at construction time).

    /// <inheritdoc />
    public override string Name => _schemaSource.Name;

    /// <inheritdoc />
    public override string Description => _schemaSource.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => _schemaSource.JsonSchema;

    /// <inheritdoc />
    public override JsonElement? ReturnJsonSchema => _schemaSource.ReturnJsonSchema;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // Resolve the service from DI. Prefer the provider from args.Services
        // (set by FunctionInvokingChatClient when .Build(provider) is used).
        // Fall back to root provider if args.Services is null or is the internal
        // EmptyServiceProvider (which can't resolve anything).
        var instance = ResolveService(arguments);

        // Create a temporary AIFunction bound to the resolved instance.
        // The (MethodInfo, object instance, options) overload does NOT dispose the instance.
        // AIFunctionFactory internally caches reflection descriptors (ReflectionAIFunctionDescriptor.GetOrCreate),
        // so this is lightweight — no schema regeneration per call.
        // This handles all parameter binding, type conversion, async unwrapping,
        // CancellationToken binding, and return value marshaling.
        var boundFunction = AIFunctionFactory.Create(_method, instance, _factoryOptions);
        return await boundFunction.InvokeAsync(arguments, cancellationToken);
    }

    private object ResolveService(AIFunctionArguments arguments)
    {
        // Try the provided service provider first (from FunctionInvokingChatClient).
        // If it fails (null provider, EmptyServiceProvider, scope validation, etc.),
        // fall back to the root provider. This is a single resolve — no test/discard pattern.
        var provided = arguments.Services;
        if (provided is not null && !ReferenceEquals(provided, _rootServiceProvider))
        {
            try
            {
                var instance = provided.GetService(_serviceType);
                if (instance is not null)
                    return instance;
            }
            catch
            {
                // Provider can't resolve (e.g. scope validation, EmptyServiceProvider).
                // Fall through to root provider.
            }
        }

        // Root provider — this is the definitive resolution.
        // If this throws (e.g. scoped from root with ValidateScopes), that's correct behavior.
        return _rootServiceProvider.GetRequiredService(_serviceType);
    }
}
```

### 1.5 Create `src/MauiAIAnnotations/ReflectionAIToolProvider.cs`

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// An <see cref="IAIToolProvider"/> that discovers AI tools via reflection,
/// scanning for methods annotated with <see cref="ExportAIFunctionAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tool definitions are created on first call to <see cref="GetTools"/> and cached.
/// Each tool is a <see cref="DependencyInjectionAIFunction"/> that resolves its service
/// from DI <b>per invocation</b>, correctly respecting DI lifetimes.
/// </para>
/// </remarks>
public class ReflectionAIToolProvider : IAIToolProvider
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IReadOnlyList<ToolRegistration> _registrations;
    private volatile IReadOnlyList<AITool>? _tools;

    /// <summary>
    /// Creates a new provider that will scan the given types for annotated methods.
    /// </summary>
    /// <param name="serviceProvider">
    /// The root DI service provider. Used as a fallback when <c>AIFunctionArguments.Services</c>
    /// cannot resolve the required service at invocation time.
    /// </param>
    /// <param name="serviceTypes">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    public ReflectionAIToolProvider(IServiceProvider serviceProvider, IEnumerable<Type> serviceTypes)
    {
        _rootServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registrations = DiscoverRegistrations(serviceTypes).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> GetTools()
    {
        // Thread-safe lazy initialization. Multiple threads may race to build,
        // but CompareExchange ensures only one result is published.
        var tools = _tools;
        if (tools is null)
        {
            tools = BuildTools();
            Interlocked.CompareExchange(ref _tools, tools, null);
            tools = _tools!;
        }
        return tools;
    }

    private IReadOnlyList<AITool> BuildTools()
    {
        var tools = new List<AITool>();

        foreach (var reg in _registrations)
        {
            tools.Add(new DependencyInjectionAIFunction(
                reg.Method,
                reg.ServiceType,
                _rootServiceProvider,
                reg.Name,
                reg.Description));
        }

        return tools.AsReadOnly();
    }

    private static IEnumerable<ToolRegistration> DiscoverRegistrations(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            // Validate: skip abstract, generic, or non-class types
            if (type.IsAbstract || type.IsGenericTypeDefinition || !type.IsClass)
                continue;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ExportAIFunctionAttribute>();
                if (attr is null)
                    continue;

                // Validate: reject unsupported method shapes
                if (method.IsGenericMethodDefinition)
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on generic method '{type.Name}.{method.Name}'. " +
                        "Close the generic parameters or use a non-generic method.");

                if (method.GetParameters().Any(p => p.ParameterType.IsByRef))
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on method '{type.Name}.{method.Name}' " +
                        "because it has ref/out/in parameters.");

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

### 1.6 Create `src/MauiAIAnnotations/ServiceCollectionExtensions.cs`

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
    [MethodImpl(MethodImplOptions.NoInlining)] // Prevent JIT inlining so GetCallingAssembly returns the correct assembly
    public static IServiceCollection AddAIToolProvider(
        this IServiceCollection services)
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
    [MethodImpl(MethodImplOptions.NoInlining)]
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

### 1.7 Create `src/MauiAIAnnotations/README.md`

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

**Change 1:** In `CreateMauiAppCore()`, after registering services but before registering ViewModels, add:

```csharp
// Register AI tool provider (discovers [ExportAIFunction] methods)
builder.Services.AddAIToolProvider(typeof(PlantDataService).Assembly);
```

**Change 2:** Update the `AddOpenAIServices` method to pass the `IServiceProvider` into the chat pipeline. This is critical — without it, `FunctionInvokingChatClient` uses an internal `EmptyServiceProvider` and `AIFunctionArguments.Services` cannot resolve any services:

```csharp
private static MauiAppBuilder AddOpenAIServices(this MauiAppBuilder builder)
{
    var aiSection = builder.Configuration.GetSection("AI");
    var apiKey = aiSection["ApiKey"];
    var endpoint = aiSection["Endpoint"];
    var deploymentName = aiSection["DeploymentName"];

    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
    {
        // AI not configured — services will use fallback behavior
        return builder;
    }

    var azureClient = new AzureOpenAIClient(
        new Uri(endpoint),
        new ApiKeyCredential(apiKey));
    var chatClient = azureClient.GetChatClient(deploymentName);

    builder.Services.AddSingleton<IChatClient>(provider =>
    {
        var lf = provider.GetRequiredService<ILoggerFactory>();
        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseLogging(lf)
            .UseFunctionInvocation()
            .Build(provider);  // ← CRITICAL: pass the service provider so args.Services is real
    });

    return builder;
}
```

The key change is `.Build(provider)` instead of `.Build()`. Without this, `ChatClientBuilder.Build()` sets `args.Services` to an internal `EmptyServiceProvider.Instance` that cannot resolve any services, and the `DependencyInjectionAIFunction`'s fallback to root provider would be needed for every call.

The `ChatViewModel` registration stays as `AddSingleton<ChatViewModel>()` — DI will resolve `IAIToolProvider` and `IChatClient?` automatically.

---

## Phase 4: Tests

### 4.0 Update `tests/MauiAIAnnotations.Tests/MauiAIAnnotations.Tests.csproj`

The existing test project needs references to the library and DI packages:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\MauiAIAnnotations\MauiAIAnnotations.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="10.4.1" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
</ItemGroup>
```

Note: We need `Microsoft.Extensions.DependencyInjection` (not just Abstractions) for `ServiceCollection` and `ServiceProviderOptions`.

### 4.1 Test helper types (in the test project)

```csharp
public class TestToolService
{
    [ExportAIFunction("test_tool", Description = "A test tool")]
    public string DoSomething([Description("input value")] string input) => $"result: {input}";

    [ExportAIFunction] // no name or description — defaults to method name
    public int GetCount() => 42;

    [ExportAIFunction("async_tool", Description = "An async tool")]
    public async Task<string> DoAsyncWork(string input)
    {
        await Task.Delay(1);
        return $"async: {input}";
    }

    // NOT a tool - no attribute
    public void InternalMethod() { }
}

// For testing disposable service handling
public class DisposableToolService : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;

    [ExportAIFunction("disposable_tool", Description = "Tool on a disposable service")]
    public string GetValue() => "value";
}

// For testing validation — generic methods should be rejected
public class InvalidToolService
{
    [ExportAIFunction("bad_generic")]
    public T GenericMethod<T>() => default!;

    [ExportAIFunction("bad_ref")]
    public void RefMethod(ref string x) { }
}
```

### 4.2 Discovery and metadata tests (`ReflectionAIToolProviderTests.cs`)

1. **Discovers methods with `[ExportAIFunction]`** — scan `TestToolService`, verify 3 tools (not `InternalMethod`)
2. **Uses custom Name from attribute** — `"test_tool"` not `"DoSomething"`
3. **Falls back to method name** — `GetCount` has no Name, should use `"GetCount"`
4. **Uses Description from attribute** — `"A test tool"` for `test_tool`
5. **Falls back to `[Description]` attribute** — verify `[Description]` on method is picked up when `ExportAIFunctionAttribute.Description` is null
6. **Ignores methods without attribute** — `InternalMethod` not in tools
7. **Rejects generic methods** — `InvalidToolService.GenericMethod<T>` throws at discovery
8. **Rejects ref/out parameters** — `InvalidToolService.RefMethod` throws at discovery
9. **Assembly scanning finds types** — `AddAIToolProvider(assembly)` discovers `TestToolService`
10. **Explicit type scanning works** — `AddAIToolProvider(typeof(TestToolService))` discovers annotated methods

### 4.2.1 Schema propagation tests

These verify that `DependencyInjectionAIFunction` correctly delegates metadata from its schema source:

11. **Tool exposes correct `JsonSchema`** — `test_tool` has a schema with `input` string parameter marked as required
12. **Tool exposes correct `ReturnJsonSchema`** — `async_tool` has a return schema for `string`
13. **Parameter `[Description]` appears in schema** — `test_tool`'s `input` parameter has `"description": "input value"` in the JSON schema
14. **Schema matches direct AIFunctionFactory output** — compare `DependencyInjectionAIFunction.JsonSchema` with `AIFunctionFactory.Create(method, dummyInstance).JsonSchema` to ensure they're equivalent

### 4.3 DI lifetime + invocation tests (`DependencyInjectionAIFunctionTests.cs`)

These tests actually invoke the tools through the DI pipeline. Use `ServiceCollection` with `ServiceProviderOptions { ValidateScopes = true }`.

**Test matrix:**

| # | Tool Service Lifetime | Provider Type | Expected Behavior |
|---|---|---|---|
| 1 | Singleton | root | ✅ Same instance on every call |
| 2 | Singleton | scoped | ✅ Same instance (singletons always return same) |
| 3 | Transient | root | ✅ Different instance each call |
| 4 | Transient | scoped | ✅ Different instance each call |
| 5 | Scoped | root + ValidateScopes | ❌ Throws `InvalidOperationException` (correct!) |
| 6 | Scoped | scoped | ✅ Same instance within scope, different across scopes |
| 7 | Scoped | root without ValidateScopes | ⚠️ Works (returns root-scoped instance) — document as user risk |

**Disposable service tests:**

| # | Test Case | Expected |
|---|---|---|
| 8 | Singleton IDisposable service invoked | ✅ Service NOT disposed after tool call |
| 9 | Transient IDisposable service invoked | ✅ Service NOT disposed by tool (DI tracks it) |
| 10 | Multiple tool calls on same singleton | Service instance is the same and still functional |

**Invocation behavior tests:**

| # | Test Case | Expected |
|---|---|---|
| 11 | Sync method returns correct value | Return value matches |
| 12 | Async `Task<T>` method returns correct value | Awaited result matches |
| 13 | Method with `CancellationToken` parameter | Token is bound correctly |
| 14 | Method with multiple parameters | All parameters bound from `AIFunctionArguments` |
| 15 | Service not registered in DI | Throws on invocation (not on `GetTools()`) |
| 16 | Single resolution per call (transient) | Exactly one instance created per invocation (no double-resolve) |

**Setup pattern:**
```csharp
var services = new ServiceCollection();
services.AddSingleton<TestToolService>();     // or AddTransient, AddScoped
services.AddAIToolProvider(typeof(TestToolService));

var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true,
    ValidateOnBuild = true,
});

var toolProvider = provider.GetRequiredService<IAIToolProvider>();
var tools = toolProvider.GetTools();
var tool = tools.First(t => t.Name == "test_tool") as AIFunction;

// Invoke with AIFunctionArguments
var args = new AIFunctionArguments(new Dictionary<string, object?>
{
    ["input"] = "hello"
});
args.Services = provider; // or a scoped provider

var result = await tool!.InvokeAsync(args);
```

### 4.4 Verify existing tests

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

1. **Discovery:** At registration time, `AddAIToolProvider()` scans types/assemblies for public instance methods with `[ExportAIFunction]`. Validates method signatures (rejects generics, ref/out params).
2. **Registration:** A `ReflectionAIToolProvider` is registered as a singleton `IAIToolProvider`.
3. **Tool creation:** On first `GetTools()` call, a `DependencyInjectionAIFunction` is created per method — a custom `AIFunction` subclass.
4. **Per-invocation resolution:** Each time the AI model calls a tool, `DependencyInjectionAIFunction`:
   - Resolves the service from DI (respecting lifetimes: singleton/transient/scoped)
   - Creates a temporary instance-bound `AIFunction` via `AIFunctionFactory.Create(MethodInfo, instance)` for parameter binding
   - Delegates invocation to the temporary function
   - Does **NOT** dispose the service (DI owns it)
5. **Caching:** The `DependencyInjectionAIFunction` objects are cached. A temporary `AIFunction` is created per invocation for parameter binding, but the service instance is never disposed by the tool.

### Service lifetime

Services are resolved from DI **per tool invocation**, not captured once at startup. This uses the `AIFunctionFactory.Create(MethodInfo, Func<AIFunctionArguments, object>, options)` overload:

- **Singleton services** — DI always returns the same instance. No issue.
- **Transient services** — DI creates a fresh instance for each tool call. Correct behavior.
- **Scoped services** — The `FunctionInvokingChatClient` provides a scoped `IServiceProvider` via `AIFunctionArguments.Services`. The factory uses that scoped provider when available, falling back to the root provider. This means scoped services work correctly in ASP.NET Core (per-request scope) and degrade gracefully in MAUI (uses root scope).

This is the same pattern MVC uses for controller activation — instance-per-invocation from DI.

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
- [ ] `DependencyInjectionAIFunction` overrides `Name`, `Description`, `JsonSchema`, `ReturnJsonSchema`
- [ ] Tool `JsonSchema` contains correct parameter names, types, and descriptions
- [ ] `MauiProgram.cs` uses `.Build(provider)` (not `.Build()`) in the chat pipeline
- [ ] `src/MauiAIAnnotations/DependencyInjectionAIFunction.cs` exists

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| Attribute on **methods** not classes | Not all methods should be tools; explicit opt-in per method |
| `IAIToolProvider` as DI interface | Clean separation; consumers don't know about discovery mechanism |
| **Custom `AIFunction` subclass** (`DependencyInjectionAIFunction`) | The `Func<AIFunctionArguments, object>` factory overload auto-disposes returned instances — unsafe for DI-owned services. Custom subclass resolves from DI + delegates invocation to a temporary instance-bound `AIFunction` (which does NOT dispose). |
| **Schema source pattern** | `AIFunctionFactory.CreateDeclaration(MethodInfo)` doesn't exist. Instead, create a dummy `AIFunction` via the `Func<>` overload at construction time — never invoked, only used to read `JsonSchema`/`ReturnJsonSchema`/`Name`/`Description`. |
| **Per-invocation service resolution** | `InvokeCoreAsync` resolves from DI each call. Singletons return same instance; transients get fresh; scoped uses `args.Services` provider. |
| **`ResolveServiceProvider` fallback** | `ChatClientBuilder.Build()` without args sets `args.Services` to `EmptyServiceProvider.Instance` (non-null, can't resolve). We test if the provider can actually resolve the service before using it, falling back to root. |
| **`.Build(provider)` in sample** | The sample `MauiProgram.cs` must pass the real `IServiceProvider` to `ChatClientBuilder.Build(provider)` so `FunctionInvokingChatClient` threads it into `AIFunctionArguments.Services`. |
| **Thread-safe `GetTools()`** | Uses `volatile` + `Interlocked.CompareExchange` instead of `??=` for safe lazy init in singleton. |
| **`[MethodImpl(NoInlining)]`** on `AddAIToolProvider()` | `Assembly.GetCallingAssembly()` returns wrong assembly if JIT inlines the method. |
| **Validation in `DiscoverRegistrations`** | Rejects generic methods and ref/out parameters at registration time with clear errors. |
| Return raw objects, not strings | M.E.AI serializes automatically; structured data is better for AI |
| `[Description]` for parameters | Standard .NET attribute; M.E.AI already reads it natively |
| Singleton `IAIToolProvider` | Tool definitions are stable; per-invocation service resolution handles lifetimes |
| Only Abstractions dependencies | Library stays lightweight; no Azure/OpenAI coupling |

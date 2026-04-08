using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MauiAIAnnotations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MauiAIAnnotations.Tests;

#region Test helper types

public class TestToolService
{
    [Description("A test tool")]
    [ExportAIFunction("test_tool")]
    public string DoSomething([Description("input value")] string input) => $"result: {input}";

    [ExportAIFunction]
    public int GetCount() => 42;

    [Description("An async tool")]
    [ExportAIFunction("async_tool")]
    public async Task<string> DoAsyncWork([Description("input value")] string input)
    {
        await Task.Delay(1);
        return $"async: {input}";
    }

    public void InternalMethod() { }
}

public class MultiParamService
{
    [Description("A tool with multiple parameters")]
    [ExportAIFunction("multi_param")]
    public string Combine(
        [Description("first name")] string firstName,
        [Description("last name")] string lastName,
        [Description("age in years")] int age)
        => $"{firstName} {lastName}, age {age}";
}

public class DisposableToolService : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;

    [Description("Tool on a disposable service")]
    [ExportAIFunction("disposable_tool")]
    public string GetValue() => "value";
}

[Description("Service-level description")]
public class DescriptionFallbackService
{
    [ExportAIFunction("fallback_desc")]
    [Description("Method-level description from DescriptionAttribute")]
    public string Work() => "done";
}

public class NoAttributeService
{
    public string DoWork() => "no attribute";
}

public abstract class AbstractService
{
    [ExportAIFunction("abstract_tool")]
    public string DoWork() => "abstract";
}

public class InvocationCounterService
{
    public int InvocationCount { get; private set; }

    [Description("Counts invocations")]
    [ExportAIFunction("counter_tool")]
    public int Increment()
    {
        InvocationCount++;
        return InvocationCount;
    }
}

public class CancellableToolService
{
    [Description("A cancellable tool")]
    [ExportAIFunction("cancellable_tool")]
    public async Task<string> CancellableWork(
        [Description("input value")] string input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        return $"done: {input}";
    }
}

public class GenericMethodService
{
    [ExportAIFunction("bad_generic")]
    public T GenericMethod<T>() => default!;
}

public class RefParameterService
{
    [ExportAIFunction("bad_ref")]
    public void RefMethod(ref string x) { }
}

public class ApprovalMixedService
{
    [Description("A safe read-only tool")]
    [ExportAIFunction("safe_read")]
    public string ReadData() => "data";

    [Description("A dangerous write tool")]
    [ExportAIFunction("dangerous_write", ApprovalRequired = true)]
    public string WriteData([Description("data to write")] string data) => $"wrote: {data}";

    [Description("Another safe tool")]
    [ExportAIFunction("another_safe")]
    public int GetCount() => 1;
}

public class AllApprovalService
{
    [Description("Needs approval")]
    [ExportAIFunction("needs_approval", ApprovalRequired = true)]
    public string DoWork() => "done";
}

public sealed class ComplexPlantRequest
{
    [Description("friendly nickname shown to the user")]
    public string Nickname { get; set; } = string.Empty;

    [Description("botanical species or variety")]
    public string Species { get; set; } = string.Empty;

    [Description("current location of the plant")]
    public string Location { get; set; } = string.Empty;

    [Description("whether the plant lives indoors")]
    public bool IsIndoor { get; set; }
}

public sealed class PlantToolResult
{
    [Description("stable identifier returned to the AI")]
    public string Id { get; set; } = string.Empty;

    [Description("nickname echoed back to the AI")]
    public string Nickname { get; set; } = string.Empty;
}

public class ComplexSchemaService
{
    [Description("Creates a plant profile from structured details.")]
    [ExportAIFunction("create_plant_profile", ApprovalRequired = true)]
    public PlantToolResult CreatePlantProfile(
        [Description("structured details for the plant profile")] ComplexPlantRequest profile,
        [Description("whether to notify the user after creation")] bool notifyUser = true) =>
        new()
        {
            Id = "plant-123",
            Nickname = profile.Nickname,
        };
}

#endregion

public class AIToolProviderDiscoveryTests
{
    [Fact]
    public void Discovers_three_tools_from_TestToolService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Equal(3, tools.Count());
    }

    [Fact]
    public void Uses_custom_name_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Contains(tools, t => t.Name == "test_tool");
    }

    [Fact]
    public void Falls_back_to_method_name_when_no_name_set()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Contains(tools, t => t.Name == "GetCount");
    }

    [Fact]
    public void Uses_description_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "test_tool");

        Assert.Equal("A test tool", tool.Description);
    }

    [Fact]
    public void Uses_description_fallback_from_DescriptionAttribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DescriptionFallbackService>();
        services.AddAITools(typeof(DescriptionFallbackService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "fallback_desc");

        Assert.Equal("Method-level description from DescriptionAttribute", tool.Description);
    }

    [Fact]
    public void Ignores_methods_without_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.DoesNotContain(tools, t => t.Name == "InternalMethod");
    }

    [Fact]
    public void Ignores_types_without_exported_functions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NoAttributeService>();
        services.AddAITools(typeof(NoAttributeService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Empty(tools);
    }

    [Fact]
    public void Skips_abstract_types()
    {
        var services = new ServiceCollection();
        services.AddAITools(typeof(AbstractService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Empty(tools);
    }
}

public class AIToolProviderRegistrationTests
{
    [Fact]
    public void Assembly_scanning_finds_types()
    {
        // Note: We use explicit types here instead of full assembly scanning
        // because the test assembly also contains intentionally invalid types
        // (GenericMethodService, RefParameterService) that would cause discovery to throw.
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddSingleton<DisposableToolService>();
        services.AddSingleton<DescriptionFallbackService>();
        services.AddAITools(typeof(TestToolService), typeof(DisposableToolService), typeof(DescriptionFallbackService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        // Should find tools from multiple types
        Assert.True(tools.Count() >= 4, $"Expected at least 4 tools, got {tools.Count()}");
        Assert.Contains(tools, t => t.Name == "test_tool");
        Assert.Contains(tools, t => t.Name == "disposable_tool");
        Assert.Contains(tools, t => t.Name == "fallback_desc");
    }

    [Fact]
    public void Explicit_type_scanning()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        

        Assert.Equal(3, tools.Count());
    }

    [Fact]
    public void Tools_are_registered_as_singletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IEnumerable<AITool>>().ToList();
        var second = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        // Individual AITool instances are singletons
        Assert.Equal(first.Count(), second.Count());
        for (int i = 0; i < first.Count; i++)
            Assert.Same(first[i], second[i]);
    }
}

public class AIToolProviderInvocationTests
{
    private static int GetIntResult(object? result)
    {
        if (result is JsonElement je)
            return je.GetInt32();
        return Convert.ToInt32(result);
    }
    [Fact]
    public async Task Singleton_invocation_uses_same_instance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?>());
        var result1 = await tool!.InvokeAsync(args);
        var result2 = await tool.InvokeAsync(args);

        // Singleton: same instance, so counter increments across calls
        Assert.Equal(1, GetIntResult(result1));
        Assert.Equal(2, GetIntResult(result2));
    }

    [Fact]
    public async Task Transient_invocation_uses_different_instance()
    {
        var services = new ServiceCollection();
        services.AddTransient<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?>());
        var result1 = await tool!.InvokeAsync(args);
        var result2 = await tool.InvokeAsync(args);

        // Transient: new instance each time, counter always 1
        Assert.Equal(1, GetIntResult(result1));
        Assert.Equal(1, GetIntResult(result2));
    }

    [Fact]
    public async Task Invocation_returns_correct_value()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "test_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "hello" });
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("result: hello", result?.ToString());
    }

    [Fact]
    public async Task Async_method_returns_correct_value()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "async_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "world" });
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("async: world", result?.ToString());
    }

    [Fact]
    public async Task Multiple_parameters_bound_correctly()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MultiParamService>();
        services.AddAITools(typeof(MultiParamService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "multi_param") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["firstName"] = "Alice",
            ["lastName"] = "Smith",
            ["age"] = 30
        });
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("Alice Smith, age 30", result?.ToString());
    }

    [Fact]
    public async Task Disposable_service_not_disposed_after_invocation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DisposableToolService>();
        services.AddAITools(typeof(DisposableToolService));
        var provider = services.BuildServiceProvider();

        var disposableService = provider.GetRequiredService<DisposableToolService>();
        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "disposable_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?>());
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("value", result?.ToString());
        Assert.False(disposableService.IsDisposed, "Service should NOT be disposed after tool invocation");
    }

    [Fact]
    public async Task Service_not_registered_throws_on_invocation()
    {
        var services = new ServiceCollection();
        // Deliberately NOT registering TestToolService in DI
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "test_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "test" });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tool!.InvokeAsync(args));
    }
}

public class AIToolProviderSchemaTests
{
    [Fact]
    public void JsonSchema_contains_parameter_info()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "test_tool");

        // The tool should be an AIFunctionDeclaration with schema
        var funcDecl = tool as AIFunctionDeclaration;
        Assert.NotNull(funcDecl);

        var schema = funcDecl.JsonSchema;
        var schemaStr = schema.ToString();

        // Schema should contain the parameter name "input"
        Assert.Contains("input", schemaStr);
    }

    [Fact]
    public void Schema_contains_parameter_description()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        
        var tool = tools.First(t => t.Name == "test_tool") as AIFunctionDeclaration;

        Assert.NotNull(tool);
        var schemaStr = tool.JsonSchema.ToString();

        // Schema should contain the description from [Description] attribute
        Assert.Contains("input value", schemaStr);
    }

    [Fact]
    public void Schema_matches_direct_AIFunctionFactory_output()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var diTool = tools.First(t => t.Name == "test_tool") as AIFunctionDeclaration;
        Assert.NotNull(diTool);

        // Create a direct AIFunction via AIFunctionFactory for comparison
        var method = typeof(TestToolService).GetMethod("DoSomething")!;
        var directTool = AIFunctionFactory.Create(method, new TestToolService(),
            new AIFunctionFactoryOptions { Name = "test_tool", Description = "A test tool" });

        // Schemas should be equivalent
        Assert.Equal(directTool.JsonSchema.ToString(), diTool.JsonSchema.ToString());
    }

    [Fact]
    public void Approval_wrapped_reflection_tool_preserves_full_ai_visible_schema()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ComplexSchemaService>();
        services.AddAITools(typeof(ComplexSchemaService));
        var provider = services.BuildServiceProvider();

        var reflectedTool = provider.GetRequiredService<IEnumerable<AITool>>()
            .Single(t => t.Name == "create_plant_profile");

        var reflectedFunction = Assert.IsAssignableFrom<AIFunction>(reflectedTool);
        Assert.IsType<ApprovalRequiredAIFunction>(reflectedTool);
        Assert.Equal("create_plant_profile", reflectedFunction.Name);
        Assert.Equal("Creates a plant profile from structured details.", reflectedFunction.Description);

        var method = typeof(ComplexSchemaService).GetMethod(nameof(ComplexSchemaService.CreatePlantProfile))!;
        var directTool = AIFunctionFactory.Create(
            method,
            new ComplexSchemaService(),
            new AIFunctionFactoryOptions
            {
                Name = "create_plant_profile",
                Description = "Creates a plant profile from structured details.",
            });

        Assert.Equal(directTool.Name, reflectedFunction.Name);
        Assert.Equal(directTool.Description, reflectedFunction.Description);
        Assert.Equal(directTool.JsonSchema.ToString(), reflectedFunction.JsonSchema.ToString());
        Assert.Equal(directTool.ReturnJsonSchema?.ToString(), reflectedFunction.ReturnJsonSchema?.ToString());

        var inputSchema = reflectedFunction.JsonSchema.ToString();
        Assert.Contains("structured details for the plant profile", inputSchema);
        Assert.Contains("friendly nickname shown to the user", inputSchema);
        Assert.Contains("botanical species or variety", inputSchema);
        Assert.Contains("current location of the plant", inputSchema);
        Assert.Contains("whether the plant lives indoors", inputSchema);
        Assert.Contains("whether to notify the user after creation", inputSchema);

        var returnSchema = reflectedFunction.ReturnJsonSchema?.ToString();
        Assert.NotNull(returnSchema);
        Assert.Contains("stable identifier returned to the AI", returnSchema!);
        Assert.Contains("nickname echoed back to the AI", returnSchema!);
    }
}

public class AIToolProviderValidationTests
{
    [Fact]
    public void Rejects_generic_methods()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GenericMethodService>();

        // Validation happens eagerly during AddAITools (at registration time)
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAITools(typeof(GenericMethodService)));
    }

    [Fact]
    public void Rejects_ref_parameters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RefParameterService>();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddAITools(typeof(RefParameterService)));
    }
}

public class AIToolProviderScopedLifetimeTests
{
    private static int GetIntResult(object? result)
    {
        if (result is JsonElement je)
            return je.GetInt32();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task Scoped_with_root_provider_and_ValidateScopes_throws()
    {
        var services = new ServiceCollection();
        services.AddScoped<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        // args.Services is null → falls back to root provider →
        // with ValidateScopes, resolving scoped from root throws
        var args = new AIFunctionArguments(new Dictionary<string, object?>());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tool!.InvokeAsync(args));
    }

    [Fact]
    public async Task Scoped_with_scoped_provider_returns_same_instance_within_scope()
    {
        var services = new ServiceCollection();
        services.AddScoped<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        // Same scope → same instance → counter increments
        using var scope = provider.CreateScope();
        var args1 = new AIFunctionArguments(new Dictionary<string, object?>()) { Services = scope.ServiceProvider };
        var args2 = new AIFunctionArguments(new Dictionary<string, object?>()) { Services = scope.ServiceProvider };

        var result1 = await tool!.InvokeAsync(args1);
        var result2 = await tool.InvokeAsync(args2);

        Assert.Equal(1, GetIntResult(result1));
        Assert.Equal(2, GetIntResult(result2));
    }

    [Fact]
    public async Task Scoped_with_different_scopes_returns_different_instances()
    {
        var services = new ServiceCollection();
        services.AddScoped<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        // Different scopes → different instances → counter resets
        using var scope1 = provider.CreateScope();
        var args1 = new AIFunctionArguments(new Dictionary<string, object?>()) { Services = scope1.ServiceProvider };
        var result1 = await tool!.InvokeAsync(args1);

        using var scope2 = provider.CreateScope();
        var args2 = new AIFunctionArguments(new Dictionary<string, object?>()) { Services = scope2.ServiceProvider };
        var result2 = await tool.InvokeAsync(args2);

        // Both should be 1 (fresh instance in each scope)
        Assert.Equal(1, GetIntResult(result1));
        Assert.Equal(1, GetIntResult(result2));
    }

    [Fact]
    public async Task Scoped_with_root_provider_without_ValidateScopes_works()
    {
        var services = new ServiceCollection();
        services.AddScoped<InvocationCounterService>();
        services.AddAITools(typeof(InvocationCounterService));

        // ValidateScopes = false (default)
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "counter_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?>());
        var result = await tool!.InvokeAsync(args);

        // Works but uses root-scoped instance (documented user risk)
        Assert.Equal(1, GetIntResult(result));
    }
}

public class AIToolProviderCancellationTests
{
    [Fact]
    public async Task CancellationToken_is_bound_correctly()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CancellableToolService>();
        services.AddAITools(typeof(CancellableToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "cancellable_tool") as AIFunction;

        // Pass a pre-cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "hello" });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await tool!.InvokeAsync(args, cts.Token));
    }

    [Fact]
    public async Task Non_cancelled_token_completes_successfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CancellableToolService>();
        services.AddAITools(typeof(CancellableToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();
        var tool = tools.First(t => t.Name == "cancellable_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "hello" });
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("done: hello", result?.ToString());
    }
}


public class MixedToolRegistrationTests
{
    [Fact]
    public void Classic_and_discovered_tools_coexist_in_IEnumerable()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();

        // 1. Attribute-discovered tools (our library)
        services.AddAITools(typeof(TestToolService));

        // 2. Classic bespoke tool (AIFunctionFactory.Create — the "old school" way)
        var classicTool = AIFunctionFactory.Create(
            () => "2024-01-15",
            "get_current_date",
            "Gets the current date");
        services.AddSingleton<AITool>(classicTool);

        var provider = services.BuildServiceProvider();
        var allTools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        // All tools should be present: 3 discovered + 1 classic = 4
        Assert.Equal(4, allTools.Count());
        Assert.Contains(allTools, t => t.Name == "test_tool");      // discovered
        Assert.Contains(allTools, t => t.Name == "GetCount");        // discovered
        Assert.Contains(allTools, t => t.Name == "async_tool");      // discovered
        Assert.Contains(allTools, t => t.Name == "get_current_date"); // classic
    }

    [Fact]
    public void Ad_hoc_tools_can_be_spread_into_options()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));

        var provider = services.BuildServiceProvider();
        var registeredTools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        // Ad-hoc tool created at call time (not registered in DI)
        var adHocTool = AIFunctionFactory.Create(
            (string query) => $"Search results for: {query}",
            "search_web",
            "Searches the web for information");

        // Spread pattern: combine registered tools with ad-hoc tools
        IList<AITool> allTools = [adHocTool, .. registeredTools];

        Assert.Equal(4, allTools.Count); // 3 discovered + 1 ad-hoc
        Assert.Contains(allTools, t => t.Name == "test_tool");
        Assert.Contains(allTools, t => t.Name == "search_web");
    }

    [Fact]
    public void Classic_tools_registered_before_AddAITools()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();

        // Register classic tool FIRST (before AddAITools)
        services.AddSingleton<AITool>(AIFunctionFactory.Create(
            () => 42,
            "answer_everything",
            "The answer to everything"));

        // Then add discovered tools
        services.AddAITools(typeof(TestToolService));

        var provider = services.BuildServiceProvider();
        var allTools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        // Order: classic first, then discovered (DI preserves registration order)
        Assert.Equal(4, allTools.Count());
        Assert.Equal("answer_everything", allTools[0].Name);
        Assert.Contains(allTools, t => t.Name == "test_tool");
    }
}

public class ApprovalRequiredTests
{
    [Fact]
    public void ApprovalRequired_true_wraps_in_ApprovalRequiredAIFunction()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AllApprovalService>();
        services.AddAITools(typeof(AllApprovalService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        Assert.Single(tools);
        Assert.IsType<ApprovalRequiredAIFunction>(tools[0]);
        Assert.Equal("needs_approval", tools[0].Name);
    }

    [Fact]
    public void ApprovalRequired_false_does_not_wrap()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        foreach (var tool in tools)
        {
            Assert.IsNotType<ApprovalRequiredAIFunction>(tool);
        }
    }

    [Fact]
    public void Mixed_service_wraps_only_flagged_methods()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApprovalMixedService>();
        services.AddAITools(typeof(ApprovalMixedService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        Assert.Equal(3, tools.Count);

        var safeRead = tools.Single(t => t.Name == "safe_read");
        var dangerousWrite = tools.Single(t => t.Name == "dangerous_write");
        var anotherSafe = tools.Single(t => t.Name == "another_safe");

        Assert.IsNotType<ApprovalRequiredAIFunction>(safeRead);
        Assert.IsType<ApprovalRequiredAIFunction>(dangerousWrite);
        Assert.IsNotType<ApprovalRequiredAIFunction>(anotherSafe);
    }

    [Fact]
    public void ApprovalRequired_preserves_tool_name_and_description()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApprovalMixedService>();
        services.AddAITools(typeof(ApprovalMixedService));
        var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();
        var wrapped = tools.Single(t => t.Name == "dangerous_write");

        Assert.IsType<ApprovalRequiredAIFunction>(wrapped);
        Assert.Equal("dangerous_write", wrapped.Name);
        Assert.Equal("A dangerous write tool", wrapped.Description);
    }
}

public class ToolApprovalPipelineTests
{
    [Fact]
    public async Task Approval_middleware_waits_for_response_and_replays_it_to_inner_client()
    {
        var request = new ToolApprovalRequestContent(
            "approval-1",
            new FunctionCallContent(
                "call-1",
                "add_plant",
                new Dictionary<string, object?> { ["nickname"] = "Fern" }));

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Added Fern.")])]);

        var coordinator = new ToolApprovalCoordinator();
        var middleware = new ToolApprovalChatClient(innerClient, coordinator);

        var responseTask = middleware.GetResponseAsync([new ChatMessage(ChatRole.User, "Add a fern")]);

        await WaitForAsync(() => coordinator.HasPendingApprovals);
        Assert.True(coordinator.TrySubmit(request.CreateResponse(approved: true)));

        var response = await responseTask;

        Assert.Equal(2, innerClient.ReceivedMessages.Count);
        Assert.Contains(
            innerClient.ReceivedMessages[1].SelectMany(static message => message.Contents),
            static content => content is ToolApprovalResponseContent { Approved: true, RequestId: "approval-1" });

        var allContents = response.Messages.SelectMany(static message => message.Contents).ToList();
        Assert.Contains(allContents, static content => content is ToolApprovalRequestContent { RequestId: "approval-1" });
        Assert.Contains(allContents, static content => content is ToolApprovalResponseContent { Approved: true, RequestId: "approval-1" });
        Assert.Contains(allContents, static content => content is TextContent { Text: "Added Fern." });
    }

    [Fact]
    public async Task Approval_middleware_preserves_edited_tool_call_arguments()
    {
        var originalCall = new FunctionCallContent(
            "call-2",
            "add_plant",
            new Dictionary<string, object?> { ["nickname"] = "Old Name" });
        var request = new ToolApprovalRequestContent("approval-2", originalCall);

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Updated nickname.")])]);

        var coordinator = new ToolApprovalCoordinator();
        var middleware = new ToolApprovalChatClient(innerClient, coordinator);

        var responseTask = middleware.GetResponseAsync([new ChatMessage(ChatRole.User, "Add a plant")]);

        await WaitForAsync(() => coordinator.HasPendingApprovals);

        var editedResponse = new ToolApprovalResponseContent(
            request.RequestId,
            approved: true,
            new FunctionCallContent(
                originalCall.CallId,
                originalCall.Name,
                new Dictionary<string, object?> { ["nickname"] = "New Name" }));

        Assert.True(coordinator.TrySubmit(editedResponse));
        await responseTask;

        var replayedResponse = Assert.IsType<ToolApprovalResponseContent>(
            innerClient.ReceivedMessages[1]
                .SelectMany(static message => message.Contents)
                .Single(static content => content is ToolApprovalResponseContent));

        var replayedCall = Assert.IsType<FunctionCallContent>(replayedResponse.ToolCall);
        Assert.Equal("New Name", replayedCall.Arguments?["nickname"]?.ToString());
    }

    [Fact]
    public async Task Approval_middleware_rejects_edited_tool_call_identity_changes()
    {
        var originalCall = new FunctionCallContent(
            "call-3",
            "add_plant",
            new Dictionary<string, object?> { ["nickname"] = "Original" });
        var request = new ToolApprovalRequestContent("approval-3", originalCall);

        var coordinator = new ToolApprovalCoordinator();
        var waitTask = coordinator.WaitForApprovalAsync([request]).AsTask();

        await WaitForAsync(() => coordinator.HasPendingApprovals);

        var invalidResponse = new ToolApprovalResponseContent(
            request.RequestId,
            approved: true,
            new FunctionCallContent(
                originalCall.CallId,
                "remove_plant",
                new Dictionary<string, object?> { ["nickname"] = "Original" }));

        Assert.False(coordinator.TrySubmit(invalidResponse));

        coordinator.CancelPending();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waitTask);
    }

    private static async Task WaitForAsync(Func<bool> predicate)
    {
        for (var i = 0; i < 50; i++)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.True(predicate(), "Timed out waiting for the expected approval state.");
    }

    [Fact]
    public async Task Approval_coordinator_supports_multiple_scopes()
    {
        var coordinator = new ToolApprovalCoordinator();
        var request1 = new ToolApprovalRequestContent(
            "approval-session-1",
            new FunctionCallContent("call-session-1", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Fern" }));
        var request2 = new ToolApprovalRequestContent(
            "approval-session-2",
            new FunctionCallContent("call-session-2", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Palm" }));

        var wait1 = coordinator.WaitForApprovalAsync("session-1", [request1]).AsTask();
        var wait2 = coordinator.WaitForApprovalAsync("session-2", [request2]).AsTask();

        await WaitForAsync(() => coordinator.HasPendingApprovals);

        Assert.True(coordinator.TrySubmit("session-1", request1.CreateResponse(approved: true)));
        Assert.False(wait1.IsCompletedSuccessfully && wait2.IsCompletedSuccessfully && !coordinator.HasPendingApprovals);

        var responses1 = await wait1;
        Assert.Single(responses1);
        Assert.True(responses1[0].Approved);

        Assert.True(coordinator.HasPendingApprovals);
        Assert.True(coordinator.TrySubmit("session-2", request2.CreateResponse(approved: false)));

        var responses2 = await wait2;
        Assert.Single(responses2);
        Assert.False(responses2[0].Approved);
        Assert.False(coordinator.HasPendingApprovals);
    }

    [Fact]
    public async Task CancelPending_scope_only_affects_that_scope()
    {
        var coordinator = new ToolApprovalCoordinator();
        var request1 = new ToolApprovalRequestContent(
            "approval-clear-1",
            new FunctionCallContent("call-clear-1", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Rosemary" }));
        var request2 = new ToolApprovalRequestContent(
            "approval-clear-2",
            new FunctionCallContent("call-clear-2", "add_plant", new Dictionary<string, object?> { ["nickname"] = "Basil" }));

        var wait1 = coordinator.WaitForApprovalAsync("session-a", [request1]).AsTask();
        var wait2 = coordinator.WaitForApprovalAsync("session-b", [request2]).AsTask();

        await WaitForAsync(() => coordinator.HasPendingApprovals);

        coordinator.CancelPending("session-a");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await wait1);

        Assert.True(coordinator.TrySubmit("session-b", request2.CreateResponse(approved: true)));
        var responses2 = await wait2;
        Assert.Single(responses2);
        Assert.True(responses2[0].Approved);
    }

    private sealed class SequenceChatClient(params ChatResponseUpdate[][] responses) : IChatClient
    {
        private readonly Queue<ChatResponseUpdate[]> _responses = new(responses);

        public List<List<ChatMessage>> ReceivedMessages { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ReceivedMessages.Add([.. messages]);

            if (!_responses.TryDequeue(out var response))
            {
                yield break;
            }

            foreach (var update in response)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}

public class ChatSessionTests
{
    [Fact]
    public async Task Headless_session_surfaces_messages_and_approvals_without_ui_state_objects()
    {
        var request = new ToolApprovalRequestContent(
            "approval-headless-1",
            new FunctionCallContent(
                "call-headless-1",
                "add_plant",
                new Dictionary<string, object?> { ["nickname"] = "Fern" }));

        var innerClient = new SequenceChatClient(
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Let me check.")])],
            [new ChatResponseUpdate(ChatRole.Assistant, [request])],
            [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Added Fern.")])]);

        var session = new ChatSession([], innerClient);
        var changes = new List<ChatSessionChangeKind>();
        session.Changed += (_, args) => changes.Add(args.Kind);

        await session.SendAsync("Add a fern");

        Assert.Equal(2, session.Messages.Count);
        Assert.Equal(ContentRole.User, session.Messages[0].Role);
        Assert.Equal(ContentRole.Assistant, session.Messages[1].Role);
        Assert.Equal("Let me check.", ((TextContent)session.Messages[1].Content).Text);
        Assert.Contains(ChatSessionChangeKind.MessageAdded, changes);
        Assert.Contains(ChatSessionChangeKind.StateChanged, changes);

        await session.SendAsync("Please continue with approval");

        Assert.True(session.HasPendingApprovals);
        Assert.Single(session.PendingApprovals);
        Assert.Equal(ToolApprovalState.Pending, session.PendingApprovals.Single().ApprovalState);
        Assert.Equal("add_plant", session.PendingApprovals.Single().ToolName);

        await session.SubmitApprovalAsync(request.CreateResponse(approved: true));

        Assert.False(session.HasPendingApprovals);
        Assert.Equal(ToolApprovalState.Approved, session.Messages.Single(m => m.Role == ContentRole.Approval).ApprovalState);
        Assert.Contains(session.Messages, static message => message.Content is TextContent { Text: "Added Fern." });
    }

    [Fact]
    public async Task Headless_session_does_not_inject_a_default_system_prompt()
    {
        var innerClient = new SequenceChatClient([new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Hello!")])]);
        var session = new ChatSession([], innerClient);

        await session.SendAsync("Hi");

        Assert.Single(innerClient.ReceivedMessages);
        Assert.DoesNotContain(innerClient.ReceivedMessages[0], static message => message.Role == ChatRole.System);
        Assert.Null(session.SystemPrompt);
    }
}

internal sealed class SequenceChatClient(params ChatResponseUpdate[][] responses) : IChatClient
{
    private readonly Queue<ChatResponseUpdate[]> _responses = new(responses);

    public List<List<ChatMessage>> ReceivedMessages { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ReceivedMessages.Add([.. messages]);

        if (!_responses.TryDequeue(out var response))
        {
            yield break;
        }

        foreach (var update in response)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

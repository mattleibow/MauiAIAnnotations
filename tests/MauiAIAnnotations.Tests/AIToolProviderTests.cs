using System.ComponentModel;
using System.Text.Json;
using MauiAIAnnotations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MauiAIAnnotations.Tests;

#region Test helper types

public class TestToolService
{
    [ExportAIFunction("test_tool", Description = "A test tool")]
    public string DoSomething([Description("input value")] string input) => $"result: {input}";

    [ExportAIFunction]
    public int GetCount() => 42;

    [ExportAIFunction("async_tool", Description = "An async tool")]
    public async Task<string> DoAsyncWork([Description("input value")] string input)
    {
        await Task.Delay(1);
        return $"async: {input}";
    }

    public void InternalMethod() { }
}

public class MultiParamService
{
    [ExportAIFunction("multi_param", Description = "A tool with multiple parameters")]
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

    [ExportAIFunction("disposable_tool", Description = "Tool on a disposable service")]
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

    [ExportAIFunction("counter_tool", Description = "Counts invocations")]
    public int Increment()
    {
        InvocationCount++;
        return InvocationCount;
    }
}

public class CancellableToolService
{
    [ExportAIFunction("cancellable_tool", Description = "A cancellable tool")]
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

#endregion

public class AIToolProviderDiscoveryTests
{
    [Fact]
    public void Discovers_three_tools_from_TestToolService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.Equal(3, tools.Count);
    }

    [Fact]
    public void Uses_custom_name_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.Contains(tools, t => t.Name == "test_tool");
    }

    [Fact]
    public void Falls_back_to_method_name_when_no_name_set()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.Contains(tools, t => t.Name == "GetCount");
    }

    [Fact]
    public void Uses_description_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
        var tool = tools.First(t => t.Name == "test_tool");

        Assert.Equal("A test tool", tool.Description);
    }

    [Fact]
    public void Uses_description_fallback_from_DescriptionAttribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DescriptionFallbackService>();
        services.AddAIToolProvider(typeof(DescriptionFallbackService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
        var tool = tools.First(t => t.Name == "fallback_desc");

        Assert.Equal("Method-level description from DescriptionAttribute", tool.Description);
    }

    [Fact]
    public void Ignores_methods_without_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.DoesNotContain(tools, t => t.Name == "InternalMethod");
    }

    [Fact]
    public void Ignores_types_without_exported_functions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NoAttributeService>();
        services.AddAIToolProvider(typeof(NoAttributeService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.Empty(tools);
    }

    [Fact]
    public void Skips_abstract_types()
    {
        var services = new ServiceCollection();
        services.AddAIToolProvider(typeof(AbstractService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

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
        services.AddAIToolProvider(typeof(TestToolService), typeof(DisposableToolService), typeof(DescriptionFallbackService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        // Should find tools from multiple types
        Assert.True(tools.Count >= 4, $"Expected at least 4 tools, got {tools.Count}");
        Assert.Contains(tools, t => t.Name == "test_tool");
        Assert.Contains(tools, t => t.Name == "disposable_tool");
        Assert.Contains(tools, t => t.Name == "fallback_desc");
    }

    [Fact]
    public void Explicit_type_scanning()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();

        Assert.Equal(3, tools.Count);
    }

    [Fact]
    public void Provider_is_registered_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IAIToolProvider>();
        var second = provider.GetRequiredService<IAIToolProvider>();

        Assert.Same(first, second);
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
        services.AddAIToolProvider(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(MultiParamService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(DisposableToolService));
        var provider = services.BuildServiceProvider();

        var disposableService = provider.GetRequiredService<DisposableToolService>();
        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
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
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tools = toolProvider.GetTools();
        var tool = tools.First(t => t.Name == "test_tool") as AIFunctionDeclaration;

        Assert.NotNull(tool);
        var schemaStr = tool.JsonSchema.ToString();

        // Schema should contain the description from [Description] attribute
        Assert.Contains("input value", schemaStr);
    }

    [Fact]
    public void GetTools_returns_same_list_on_multiple_calls()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var first = toolProvider.GetTools();
        var second = toolProvider.GetTools();

        Assert.Same(first, second);
    }

    [Fact]
    public void Schema_matches_direct_AIFunctionFactory_output()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAIToolProvider(typeof(TestToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var diTool = toolProvider.GetTools().First(t => t.Name == "test_tool") as AIFunctionDeclaration;
        Assert.NotNull(diTool);

        // Create a direct AIFunction via AIFunctionFactory for comparison
        var method = typeof(TestToolService).GetMethod("DoSomething")!;
        var directTool = AIFunctionFactory.Create(method, new TestToolService(),
            new AIFunctionFactoryOptions { Name = "test_tool", Description = "A test tool" });

        // Schemas should be equivalent
        Assert.Equal(directTool.JsonSchema.ToString(), diTool.JsonSchema.ToString());
    }
}

public class AIToolProviderValidationTests
{
    [Fact]
    public void Rejects_generic_methods()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GenericMethodService>();
        services.AddAIToolProvider(typeof(GenericMethodService));
        var provider = services.BuildServiceProvider();

        // Validation happens when the provider is resolved (singleton factory runs DiscoverRegistrations)
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IAIToolProvider>());
    }

    [Fact]
    public void Rejects_ref_parameters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RefParameterService>();
        services.AddAIToolProvider(typeof(RefParameterService));
        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IAIToolProvider>());
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
        services.AddAIToolProvider(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "counter_tool") as AIFunction;

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
        services.AddAIToolProvider(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "counter_tool") as AIFunction;

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
        services.AddAIToolProvider(typeof(InvocationCounterService));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "counter_tool") as AIFunction;

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
        services.AddAIToolProvider(typeof(InvocationCounterService));

        // ValidateScopes = false (default)
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "counter_tool") as AIFunction;

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
        services.AddAIToolProvider(typeof(CancellableToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "cancellable_tool") as AIFunction;

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
        services.AddAIToolProvider(typeof(CancellableToolService));
        var provider = services.BuildServiceProvider();

        var toolProvider = provider.GetRequiredService<IAIToolProvider>();
        var tool = toolProvider.GetTools().First(t => t.Name == "cancellable_tool") as AIFunction;

        var args = new AIFunctionArguments(new Dictionary<string, object?> { ["input"] = "hello" });
        var result = await tool!.InvokeAsync(args);

        Assert.Equal("done: hello", result?.ToString());
    }
}

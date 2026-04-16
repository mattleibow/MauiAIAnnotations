using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionRegistrationTests
{
    [Fact]
    public void Multiple_source_types_are_aggregated()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddSingleton<DisposableToolService>();
        services.AddSingleton<DescriptionFallbackService>();
        services.AddAITools<RegistrationTestToolContext>();
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.True(tools.Count() >= 4, $"Expected at least 4 tools, got {tools.Count()}");
        Assert.Contains(tools, t => t.Name == "test_tool");
        Assert.Contains(tools, t => t.Name == "disposable_tool");
        Assert.Contains(tools, t => t.Name == "fallback_desc");
    }

    [Fact]
    public void Explicit_type_scanning_registers_expected_tools()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools<TestToolContext>();
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Equal(3, tools.Count());
    }

    [Fact]
    public void Tools_are_registered_as_singletons()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools<TestToolContext>();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IEnumerable<AITool>>().ToList();
        var second = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Same(first[i], second[i]);
        }
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIToolContextTests
{
    [Fact]
    public void Context_creates_tools_from_source_types()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools<TestToolContext>();
        using var serviceProvider = services.BuildServiceProvider();

        var tools = serviceProvider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Equal(3, tools.Count());
        Assert.Contains(tools, t => t.Name == "test_tool");
    }

    [Fact]
    public void Default_instance_returns_same_context()
    {
        var first = TestToolContext.Default;
        var second = TestToolContext.Default;

        Assert.Same(first, second);
    }

    [Fact]
    public void Context_with_multiple_sources_aggregates_tools()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddSingleton<MultiParamService>();
        using var serviceProvider = services.BuildServiceProvider();

        var tools = CompositeToolContext.Default.GetTools(serviceProvider);

        Assert.Equal(4, tools.Count); // 3 from TestToolService + 1 from MultiParamService
        Assert.Contains(tools, t => t.Name == "test_tool");
        Assert.Contains(tools, t => t.Name == "multi_param");
    }
}

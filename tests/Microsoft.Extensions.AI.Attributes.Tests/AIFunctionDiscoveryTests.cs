using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionDiscoveryTests
{
    [Fact]
    public void Discovers_three_tools_from_test_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Equal(3, tools.Count());
    }

    [Fact]
    public void Uses_custom_name_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Contains(tools, t => t.Name == "test_tool");
    }

    [Fact]
    public void Falls_back_to_method_name_when_no_name_is_set()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Contains(tools, t => t.Name == "GetCount");
    }

    [Fact]
    public void Uses_description_from_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tool = provider.GetRequiredService<IEnumerable<AITool>>().First(t => t.Name == "test_tool");

        Assert.Equal("A test tool", tool.Description);
    }

    [Fact]
    public void Uses_description_fallback_from_description_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DescriptionFallbackService>();
        services.AddAITools(typeof(DescriptionFallbackService));
        using var provider = services.BuildServiceProvider();

        var tool = provider.GetRequiredService<IEnumerable<AITool>>().First(t => t.Name == "fallback_desc");

        Assert.Equal("Method-level description from DescriptionAttribute", tool.Description);
    }

    [Fact]
    public void Ignores_methods_without_export_attribute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.DoesNotContain(tools, t => t.Name == "InternalMethod");
    }

    [Fact]
    public void Ignores_types_without_exported_functions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<NoAttributeService>();
        services.AddAITools(typeof(NoAttributeService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Empty(tools);
    }

    [Fact]
    public void Skips_abstract_types()
    {
        var services = new ServiceCollection();
        services.AddAITools(typeof(AbstractService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Empty(tools);
    }
}

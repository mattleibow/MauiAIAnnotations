using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionProviderTests
{
    [Fact]
    public void Provider_can_register_tools_from_explicit_types()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();

        AIFunctionProvider.Default.AddAITools(services, typeof(TestToolService));
        using var serviceProvider = services.BuildServiceProvider();

        var tools = serviceProvider.GetRequiredService<IEnumerable<AITool>>();

        Assert.Equal(3, tools.Count());
        Assert.Contains(tools, t => t.Name == "test_tool");
    }

    [Fact]
    public void Provider_returns_root_assembly_when_querying_relevant_assemblies()
    {
        var assemblies = AIFunctionProvider.Default.GetRelevantAssemblies(typeof(TestToolService).Assembly);

        Assert.Contains(typeof(TestToolService).Assembly, assemblies);
    }
}

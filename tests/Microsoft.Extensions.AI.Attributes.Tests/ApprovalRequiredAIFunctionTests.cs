using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class ApprovalRequiredAIFunctionTests
{
    [Fact]
    public void Approval_required_true_wraps_in_approval_required_ai_function()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AllApprovalService>();
        services.AddAITools(typeof(AllApprovalService));
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        Assert.Single(tools);
        Assert.IsType<ApprovalRequiredAIFunction>(tools[0]);
        Assert.Equal("needs_approval", tools[0].Name);
    }

    [Fact]
    public void Approval_required_false_does_not_wrap()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        foreach (var tool in provider.GetRequiredService<IEnumerable<AITool>>())
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
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetRequiredService<IEnumerable<AITool>>().ToList();

        Assert.Equal(3, tools.Count);
        Assert.IsNotType<ApprovalRequiredAIFunction>(tools.Single(t => t.Name == "safe_read"));
        Assert.IsType<ApprovalRequiredAIFunction>(tools.Single(t => t.Name == "dangerous_write"));
        Assert.IsNotType<ApprovalRequiredAIFunction>(tools.Single(t => t.Name == "another_safe"));
    }

    [Fact]
    public void Approval_required_preserves_tool_name_and_description()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ApprovalMixedService>();
        services.AddAITools(typeof(ApprovalMixedService));
        using var provider = services.BuildServiceProvider();

        var wrapped = provider.GetRequiredService<IEnumerable<AITool>>().Single(t => t.Name == "dangerous_write");

        Assert.IsType<ApprovalRequiredAIFunction>(wrapped);
        Assert.Equal("dangerous_write", wrapped.Name);
        Assert.Equal("A dangerous write tool", wrapped.Description);
    }
}

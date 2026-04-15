using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionSchemaTests
{
    [Fact]
    public void Json_schema_contains_parameter_info()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tool = provider.GetRequiredService<IEnumerable<AITool>>().First(t => t.Name == "test_tool");
        var function = Assert.IsAssignableFrom<AIFunctionDeclaration>(tool);

        Assert.Contains("input", function.JsonSchema.ToString());
    }

    [Fact]
    public void Schema_contains_parameter_description()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var tool = Assert.IsAssignableFrom<AIFunctionDeclaration>(
            provider.GetRequiredService<IEnumerable<AITool>>().First(t => t.Name == "test_tool"));

        Assert.Contains("input value", tool.JsonSchema.ToString());
    }

    [Fact]
    public void Schema_matches_direct_factory_output()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TestToolService>();
        services.AddAITools(typeof(TestToolService));
        using var provider = services.BuildServiceProvider();

        var diTool = Assert.IsAssignableFrom<AIFunctionDeclaration>(
            provider.GetRequiredService<IEnumerable<AITool>>().First(t => t.Name == "test_tool"));

        var method = typeof(TestToolService).GetMethod(nameof(TestToolService.DoSomething))!;
        var directTool = AIFunctionFactory.Create(
            method,
            new TestToolService(),
            new AIFunctionFactoryOptions { Name = "test_tool", Description = "A test tool" });

        Assert.Equal(directTool.JsonSchema.ToString(), diTool.JsonSchema.ToString());
    }

    [Fact]
    public void Approval_wrapped_tools_preserve_full_ai_visible_schema()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ComplexSchemaService>();
        services.AddAITools(typeof(ComplexSchemaService));
        using var provider = services.BuildServiceProvider();

        var reflectedTool = provider.GetRequiredService<IEnumerable<AITool>>().Single(t => t.Name == "create_plant_profile");
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

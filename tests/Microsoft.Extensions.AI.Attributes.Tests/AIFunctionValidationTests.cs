using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionValidationTests
{
    [Fact]
    public void Rejects_generic_methods()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GenericMethodService>();

        Assert.Throws<InvalidOperationException>(() => services.AddAITools(typeof(GenericMethodService)));
    }

    [Fact]
    public void Rejects_ref_parameters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<RefParameterService>();

        Assert.Throws<InvalidOperationException>(() => services.AddAITools(typeof(RefParameterService)));
    }
}

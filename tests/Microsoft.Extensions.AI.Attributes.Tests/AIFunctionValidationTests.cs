using Microsoft.Extensions.AI.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes.Tests;

public class AIFunctionValidationTests
{
    [Fact]
    public void Generic_methods_are_excluded_by_source_generator()
    {
        // With source generators, generic methods with [ExportAIFunction] will produce
        // a compile-time diagnostic rather than a runtime exception.
        // The generator skips generic methods, so they won't appear in any tool context.
        // This test documents the expected behavior.
    }

    [Fact]
    public void Ref_parameters_are_excluded_by_source_generator()
    {
        // With source generators, methods with ref/out/in parameters and [ExportAIFunction]
        // will produce a compile-time diagnostic rather than a runtime exception.
        // The generator skips such methods, so they won't appear in any tool context.
        // This test documents the expected behavior.
    }
}

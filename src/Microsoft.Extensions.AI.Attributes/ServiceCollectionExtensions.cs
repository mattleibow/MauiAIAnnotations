using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Extension methods for registering AI tools discovered from annotated service methods.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static AIFunctionProvider Provider => AIFunctionProvider.Default;

    /// <summary>
     /// Scans the calling assembly and its referenced assemblies for types containing methods
     /// annotated with <see cref="ExportAIFunctionAttribute"/> and registers the discovered
    /// <see cref="AITool"/> instances in DI. Consumers inject <c>IEnumerable&lt;AITool&gt;</c>
    /// to receive all tools.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAITools(this IServiceCollection services)
    {
        return Provider.AddFromCallingAssembly(services, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Scans the specified assemblies for types containing methods annotated with
    /// <see cref="ExportAIFunctionAttribute"/> and registers the discovered <see cref="AITool"/>
    /// instances in DI. Consumers inject <c>IEnumerable&lt;AITool&gt;</c> to receive all tools.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAITools(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        return Provider.AddAITools(services, assemblies);
    }

    /// <summary>
    /// Registers <see cref="AITool"/> instances from the specified types' annotated methods.
    /// Each method with <see cref="ExportAIFunctionAttribute"/> becomes a singleton <see cref="AITool"/>
    /// in DI. Consumers inject <c>IEnumerable&lt;AITool&gt;</c> to receive all tools.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="types">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAITools(
        this IServiceCollection services,
        params Type[] types)
    {
        return Provider.AddAITools(services, types);
    }
}

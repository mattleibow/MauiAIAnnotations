using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Extension methods for registering AI tools from source-generated <see cref="AIToolContext"/> subclasses.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AI tools from a source-generated <see cref="AIToolContext"/>.
    /// Each tool is registered as a singleton <see cref="AITool"/> in DI.
    /// Consumers inject <c>IEnumerable&lt;AITool&gt;</c> to receive all tools.
    /// </summary>
    /// <typeparam name="TContext">
    /// A source-generated <see cref="AIToolContext"/> subclass decorated with
    /// <see cref="AIToolSourceAttribute"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAITools<TContext>(this IServiceCollection services)
        where TContext : AIToolContext, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        new TContext().RegisterTools(services);
        return services;
    }

    /// <summary>
    /// Registers AI tools from a source-generated <see cref="AIToolContext"/> under a
    /// keyed service registration. Use this to create multiple independent tool sets.
    /// </summary>
    /// <typeparam name="TContext">
    /// A source-generated <see cref="AIToolContext"/> subclass decorated with
    /// <see cref="AIToolSourceAttribute"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The service key for keyed DI registration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAITools<TContext>(this IServiceCollection services, string key)
        where TContext : AIToolContext, new()
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);
        new TContext().RegisterTools(services, key);
        return services;
    }
}

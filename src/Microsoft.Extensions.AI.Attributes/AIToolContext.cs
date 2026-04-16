using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Base class for source-generated AI tool contexts. Subclasses decorated with
/// <see cref="AIToolSourceAttribute"/> have their tool registration methods
/// implemented by the source generator at compile time.
/// </summary>
/// <remarks>
/// This follows the same pattern as <c>System.Text.Json.Serialization.JsonSerializerContext</c>:
/// declare a partial class, decorate it with attributes, and the source generator fills in the
///  no runtime reflection needed for discovery.implementation 
/// </remarks>
public abstract class AIToolContext
{
    /// <summary>
    /// Returns the AI tools defined by this context, bound to the given service provider
    /// for resolving service instances per invocation.
    /// </summary>
    /// <param name="serviceProvider">
    /// The root service provider used to resolve service dependencies for each tool invocation.
    /// </param>
    /// <returns>The list of AI tools produced by this context.</returns>
    public abstract IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider);

    /// <summary>
    /// Registers all tools from this context as individual <see cref="AITool"/> singleton
    /// services in the given service collection.
    /// </summary>
    /// <param name="services">The service collection to register tools in.</param>
    public abstract void RegisterTools(IServiceCollection services);

    /// <summary>
    /// Registers all tools from this context as individual keyed <see cref="AITool"/> singleton
    /// services in the given service collection.
    /// </summary>
    /// <param name="services">The service collection to register tools in.</param>
    /// <param name="key">The service key for keyed DI registration.</param>
    public abstract void RegisterTools(IServiceCollection services, string key);

    /// <summary>
    /// Creates an AI tool that resolves its service instance from DI on each invocation.
    /// Called by generated  not intended for direct use.code 
    /// </summary>
    protected static AITool CreateDITool(
        IServiceProvider rootServiceProvider,
        Type serviceType,
        string methodName,
        string toolName,
        string? description,
        bool approvalRequired)
    {
        var method = serviceType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Method '{methodName}' not found on type {serviceType.FullName}.");

        AIFunction function = new DependencyInjectionAIFunction(
            method,
            serviceType,
            rootServiceProvider,
            toolName,
            description);

        return approvalRequired
            ? new ApprovalRequiredAIFunction(function)
            : function;
    }

    /// <summary>
    /// Registers a single AI tool as a singleton service using a factory
    /// that creates a DI-resolving tool. Called by generated code.
    /// </summary>
    protected static void RegisterDITool(
        IServiceCollection services,
        Type serviceType,
        string methodName,
        string toolName,
        string? description,
        bool approvalRequired)
    {
        services.AddSingleton<AITool>(sp =>
            CreateDITool(sp, serviceType, methodName, toolName, description, approvalRequired));
    }

    /// <summary>
    /// Registers a single AI tool as a keyed singleton service. Called by generated code.
    /// </summary>
    protected static void RegisterKeyedDITool(
        IServiceCollection services,
        string key,
        Type serviceType,
        string methodName,
        string toolName,
        string? description,
        bool approvalRequired)
    {
        services.AddKeyedSingleton<AITool>(key, (sp, _) =>
            CreateDITool(sp, serviceType, methodName, toolName, description, approvalRequired));
    }
}

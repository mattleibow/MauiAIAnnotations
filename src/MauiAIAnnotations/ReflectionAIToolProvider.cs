using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// An <see cref="IAIToolProvider"/> that discovers AI tools via reflection,
/// scanning for methods annotated with <see cref="ExportAIFunctionAttribute"/>.
/// </summary>
/// <remarks>
/// Service instances are resolved from DI on each invocation via
/// <see cref="DependencyInjectionAIFunction"/>, which avoids capturing a single
/// instance and respects DI lifetimes (singleton, transient, scoped).
/// The tool list is cached for the lifetime of the provider.
/// </remarks>
public class ReflectionAIToolProvider : IAIToolProvider
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IReadOnlyList<ToolRegistration> _registrations;
    private volatile IReadOnlyList<AITool>? _tools;

    /// <summary>
    /// Creates a new provider that will scan the given types for annotated methods.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider used to resolve service instances.</param>
    /// <param name="serviceTypes">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    public ReflectionAIToolProvider(IServiceProvider serviceProvider, IEnumerable<Type> serviceTypes)
    {
        _rootServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registrations = DiscoverRegistrations(serviceTypes).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<AITool> GetTools()
    {
        var tools = _tools;
        if (tools is null)
        {
            tools = BuildTools();
            Interlocked.CompareExchange(ref _tools, tools, null);
            tools = _tools!;
        }
        return tools;
    }

    private IReadOnlyList<AITool> BuildTools()
    {
        var tools = new List<AITool>();
        foreach (var reg in _registrations)
        {
            tools.Add(new DependencyInjectionAIFunction(
                reg.Method,
                reg.ServiceType,
                _rootServiceProvider,
                reg.Name,
                reg.Description));
        }
        return tools.AsReadOnly();
    }

    private static IEnumerable<ToolRegistration> DiscoverRegistrations(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition || !type.IsClass)
                continue;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ExportAIFunctionAttribute>();
                if (attr is null)
                    continue;

                if (method.IsGenericMethodDefinition)
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on generic method '{type.Name}.{method.Name}'.");

                if (method.GetParameters().Any(p => p.ParameterType.IsByRef))
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on method '{type.Name}.{method.Name}' because it has ref/out/in parameters.");

                var name = attr.Name ?? method.Name;
                var description = attr.Description
                    ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description;

                yield return new ToolRegistration(type, method, name, description);
            }
        }
    }

    internal sealed record ToolRegistration(
        Type ServiceType,
        MethodInfo Method,
        string Name,
        string? Description);
}

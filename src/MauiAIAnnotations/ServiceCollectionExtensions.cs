using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MauiAIAnnotations;

/// <summary>
/// Extension methods for registering AI tools discovered from annotated service methods.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared approval coordinator used by the legacy blocking approval middleware.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">The lifetime for the coordinator service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddToolApprovalCoordinator(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.TryAdd(new ServiceDescriptor(typeof(IToolApprovalCoordinator), typeof(ToolApprovalCoordinator), lifetime));
        return services;
    }

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
        var callingAssembly = Assembly.GetCallingAssembly();
        var assemblies = GetRelevantAssemblies(callingAssembly);
        return services.AddAITools([.. assemblies]);
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
            assemblies = [Assembly.GetCallingAssembly()];

        var types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(TypeHasExportedFunctions)
            .ToList();

        return services.AddAITools([.. types]);
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
        var registrations = DiscoverRegistrations(types);

        foreach (var reg in registrations)
        {
            // Capture values for closure
            var method = reg.Method;
            var serviceType = reg.ServiceType;
            var name = reg.Name;
            var description = reg.Description;
            var approvalRequired = reg.ApprovalRequired;

            services.AddSingleton<AITool>(sp =>
            {
                AIFunction fn = new DependencyInjectionAIFunction(method, serviceType, sp, name, description);
                return approvalRequired ? new ApprovalRequiredAIFunction(fn) : fn;
            });
        }

        return services;
    }

    private static bool TypeHasExportedFunctions(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.GetCustomAttribute<ExportAIFunctionAttribute>() is not null);
    }

    private static List<Assembly> GetRelevantAssemblies(Assembly root)
    {
        var result = new List<Assembly> { root };
        foreach (var refName in root.GetReferencedAssemblies())
        {
            try
            {
                var asm = Assembly.Load(refName);
                if (asm.GetExportedTypes().Any(t => t.IsClass && !t.IsAbstract && TypeHasExportedFunctions(t)))
                    result.Add(asm);
            }
            catch
            {
                // Skip assemblies that can't be loaded
            }
        }
        return result;
    }

    private static List<ToolRegistration> DiscoverRegistrations(IEnumerable<Type> types)
    {
        var registrations = new List<ToolRegistration>();

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

                registrations.Add(new ToolRegistration(type, method, name, description, attr.ApprovalRequired));
            }
        }

        return registrations;
    }

    private sealed record ToolRegistration(
        Type ServiceType,
        MethodInfo Method,
        string Name,
        string? Description,
        bool ApprovalRequired);
}

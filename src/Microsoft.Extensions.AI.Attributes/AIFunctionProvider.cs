using System.ComponentModel;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Central reflection-based provider for discovering AI functions from annotated service methods.
/// Keeping discovery here makes it easy to swap in a source-generated implementation later.
/// </summary>
public sealed class AIFunctionProvider
{
    /// <summary>
    /// Gets the default reflection-based provider.
    /// </summary>
    public static AIFunctionProvider Default { get; } = new();

    /// <summary>
    /// Scans the calling assembly and its relevant references for annotated AI functions and registers them in DI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="callingAssembly">The assembly requesting registration.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddFromCallingAssembly(IServiceCollection services, Assembly callingAssembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(callingAssembly);

        var assemblies = GetRelevantAssemblies(callingAssembly);
        return AddAITools(services, [.. assemblies]);
    }

    /// <summary>
    /// Scans the specified assemblies for types containing methods annotated with
    /// <see cref="ExportAIFunctionAttribute"/> and registers the discovered <see cref="AITool"/> instances.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddAITools(IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var types = assemblies
            .SelectMany(GetExportedTypes)
            .Where(static t => t.IsClass && !t.IsAbstract)
            .Where(TypeHasExportedFunctions)
            .ToArray();

        return AddAITools(services, types);
    }

    /// <summary>
    /// Registers <see cref="AITool"/> instances from the specified types' annotated methods.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="types">The types to scan for <see cref="ExportAIFunctionAttribute"/> methods.</param>
    /// <returns>The service collection for chaining.</returns>
    public IServiceCollection AddAITools(IServiceCollection services, params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(types);

        foreach (var registration in DiscoverRegistrations(types))
        {
            RegisterTool(services, registration);
        }

        return services;
    }

    /// <summary>
    /// Returns the root assembly plus referenced assemblies that expose annotated AI functions.
    /// </summary>
    /// <param name="root">The root assembly to inspect.</param>
    /// <returns>A list of relevant assemblies to scan.</returns>
    public IReadOnlyList<Assembly> GetRelevantAssemblies(Assembly root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var assemblies = new List<Assembly> { root };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            root.FullName ?? root.GetName().Name ?? root.ToString(),
        };

        foreach (var reference in root.GetReferencedAssemblies())
        {
            try
            {
                var assembly = Assembly.Load(reference);
                var key = assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString();

                if (!seen.Add(key))
                {
                    continue;
                }

                if (GetExportedTypes(assembly).Any(static t => t.IsClass && !t.IsAbstract && TypeHasExportedFunctions(t)))
                {
                    assemblies.Add(assembly);
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (FileLoadException)
            {
            }
            catch (BadImageFormatException)
            {
            }
        }

        return assemblies;
    }

    private static IEnumerable<Type> GetExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(static t => t is { IsVisible: true })
                .Cast<Type>();
        }
    }

    private static void RegisterTool(IServiceCollection services, ToolRegistration registration)
    {
        services.AddSingleton<AITool>(sp =>
        {
            AIFunction function = new DependencyInjectionAIFunction(
                registration.Method,
                registration.ServiceType,
                sp,
                registration.Name,
                registration.Description);

            return registration.ApprovalRequired
                ? new ApprovalRequiredAIFunction(function)
                : function;
        });
    }

    private static bool TypeHasExportedFunctions(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(static method => method.GetCustomAttribute<ExportAIFunctionAttribute>() is not null);
    }

    private static IReadOnlyList<ToolRegistration> DiscoverRegistrations(IEnumerable<Type> types)
    {
        var registrations = new List<ToolRegistration>();

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsGenericTypeDefinition || !type.IsClass)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<ExportAIFunctionAttribute>();
                if (attribute is null)
                {
                    continue;
                }

                if (method.IsGenericMethodDefinition)
                {
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on generic method '{type.Name}.{method.Name}'.");
                }

                if (method.GetParameters().Any(static parameter => parameter.ParameterType.IsByRef))
                {
                    throw new InvalidOperationException(
                        $"[ExportAIFunction] is not supported on method '{type.Name}.{method.Name}' because it has ref/out/in parameters.");
                }

                registrations.Add(new ToolRegistration(
                    type,
                    method,
                    attribute.Name ?? method.Name,
                    attribute.Description ?? method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                    attribute.ApprovalRequired));
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

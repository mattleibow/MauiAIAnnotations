using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MauiAIAnnotations;

public static class ServiceCollectionExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAIToolProvider(this IServiceCollection services)
    {
        return services.AddAIToolProvider(Assembly.GetCallingAssembly());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddAIToolProvider(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        var types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(TypeHasExportedFunctions)
            .ToList();

        return services.AddAIToolProvider(types.ToArray());
    }

    public static IServiceCollection AddAIToolProvider(this IServiceCollection services, params Type[] types)
    {
        var serviceTypes = types.ToList();
        services.TryAddSingleton<IAIToolProvider>(sp =>
            new ReflectionAIToolProvider(sp, serviceTypes));
        return services;
    }

    private static bool TypeHasExportedFunctions(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Any(m => m.GetCustomAttribute<ExportAIFunctionAttribute>() is not null);
    }
}

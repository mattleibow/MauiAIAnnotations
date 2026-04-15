using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

internal sealed class DependencyInjectionAIFunction : AIFunction
{
    private readonly MethodInfo _method;
    private readonly Type _serviceType;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly AIFunctionFactoryOptions _factoryOptions;
    private readonly AIFunction _schemaSource;

    public DependencyInjectionAIFunction(
        MethodInfo method,
        Type serviceType,
        IServiceProvider rootServiceProvider,
        string name,
        string? description)
    {
        _method = method;
        _serviceType = serviceType;
        _rootServiceProvider = rootServiceProvider;

        _factoryOptions = new AIFunctionFactoryOptions
        {
            Name = name,
            Description = description,
        };

        // Schema source: created with a dummy factory, NEVER invoked.
        // Used only for metadata (Name, Description, JsonSchema, ReturnJsonSchema).
        _schemaSource = AIFunctionFactory.Create(
            _method,
            static _ => throw new InvalidOperationException("Schema source should not be invoked."),
            _factoryOptions);
    }

    public override string Name => _schemaSource.Name;
    public override string Description => _schemaSource.Description;
    public override JsonElement JsonSchema => _schemaSource.JsonSchema;
    public override JsonElement? ReturnJsonSchema => _schemaSource.ReturnJsonSchema;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var instance = ResolveService(arguments);
        var boundFunction = AIFunctionFactory.Create(_method, instance, _factoryOptions);
        return await boundFunction.InvokeAsync(arguments, cancellationToken);
    }

    private object ResolveService(AIFunctionArguments arguments)
    {
        var provided = arguments.Services;
        if (provided is not null && !ReferenceEquals(provided, _rootServiceProvider))
        {
            try
            {
                var instance = provided.GetService(_serviceType);
                if (instance is not null)
                    return instance;
            }
            catch
            {
                // Fall through to root
            }
        }
        return _rootServiceProvider.GetRequiredService(_serviceType);
    }
}

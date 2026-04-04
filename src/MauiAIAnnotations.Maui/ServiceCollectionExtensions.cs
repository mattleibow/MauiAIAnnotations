using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MauiAIAnnotations.Maui;

/// <summary>
/// Extension methods for registering AI chat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ChatViewModel"/> in DI.
    /// Requires <c>IEnumerable&lt;AITool&gt;</c> and <c>IChatClient</c> to be registered.
    /// </summary>
    public static IServiceCollection AddAIChat(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.TryAdd(new ServiceDescriptor(typeof(ChatViewModel), typeof(ChatViewModel), lifetime));
        return services;
    }
}

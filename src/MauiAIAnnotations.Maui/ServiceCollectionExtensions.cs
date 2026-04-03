using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MauiAIAnnotations.Maui;

/// <summary>
/// Extension methods for registering AI chat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ChatViewModel"/> as a singleton in DI.
    /// Requires <c>IEnumerable&lt;AITool&gt;</c> and <c>IChatClient</c> to be registered.
    /// </summary>
    public static IServiceCollection AddAIChat(this IServiceCollection services)
    {
        services.AddSingleton<ChatViewModel>();
        return services;
    }
}

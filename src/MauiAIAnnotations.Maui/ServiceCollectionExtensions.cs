using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MauiAIAnnotations.Maui.Chat;
using MauiAIAnnotations.Maui.ViewModels;

namespace MauiAIAnnotations.Maui;

/// <summary>
/// Extension methods for registering AI chat services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the session-scoped chat state object used by <see cref="Controls.ChatPanelControl"/>.
    /// Requires <c>IEnumerable&lt;AITool&gt;</c> and <c>IChatClient</c> to be registered.
    /// </summary>
    /// <remarks>
    /// <paramref name="lifetime"/> defaults to <see cref="ServiceLifetime.Transient"/> so each page or window can
    /// resolve its own chat session. If you want one shared conversation across the whole app, pass
    /// <see cref="ServiceLifetime.Singleton"/> explicitly.
    /// </remarks>
    public static IServiceCollection AddAIChat(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        services.TryAdd(new ServiceDescriptor(typeof(ChatSession), typeof(ChatSession), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ChatViewModel), typeof(ChatViewModel), lifetime));
        return services;
    }
}

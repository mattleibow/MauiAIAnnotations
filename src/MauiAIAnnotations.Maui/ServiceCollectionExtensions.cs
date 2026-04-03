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
    /// Consumers inject <see cref="ChatViewModel"/> (or a subclass) into their pages.
    /// Requires <c>IEnumerable&lt;AITool&gt;</c> and <c>IChatClient</c> to be registered.
    /// </summary>
    public static IServiceCollection AddAIChat(this IServiceCollection services)
    {
        services.AddSingleton<ChatViewModel>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ChatViewModel"/> subclass as a singleton in DI.
    /// Use this when you have a custom ViewModel that adds additional tools or behavior.
    /// </summary>
    public static IServiceCollection AddAIChat<TChatViewModel>(this IServiceCollection services)
        where TChatViewModel : ChatViewModel
    {
        services.AddSingleton<ChatViewModel, TChatViewModel>();
        services.AddSingleton<TChatViewModel>(sp => (TChatViewModel)sp.GetRequiredService<ChatViewModel>());
        return services;
    }
}

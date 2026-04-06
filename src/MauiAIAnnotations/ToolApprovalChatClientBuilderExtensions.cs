using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace MauiAIAnnotations;

/// <summary>
/// Provides extension methods for wiring approval handling into a chat-client pipeline.
/// </summary>
public static class ToolApprovalChatClientBuilderExtensions
{
    /// <summary>
    /// Adds the MauiAIAnnotations approval middleware to the chat pipeline.
    /// </summary>
    /// <remarks>
    /// Call this <em>before</em> <see cref="FunctionInvokingChatClientBuilderExtensions.UseFunctionInvocation(ChatClientBuilder, Microsoft.Extensions.Logging.ILoggerFactory?, Action{FunctionInvokingChatClient}?)"/>
    /// so the approval middleware wraps the MEAI function invoker and can pause when approval-required tools are proposed.
    /// </remarks>
    /// <param name="builder">The chat-client builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ChatClientBuilder UseMauiToolApproval(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            var coordinator = services.GetService<IToolApprovalCoordinator>();
            if (coordinator is null)
            {
                throw new InvalidOperationException(
                    "UseMauiToolApproval requires IToolApprovalCoordinator to be registered. Call services.AddToolApprovalCoordinator() or services.AddAIChat() first.");
            }

            return new ToolApprovalChatClient(innerClient, coordinator);
        });
    }
}

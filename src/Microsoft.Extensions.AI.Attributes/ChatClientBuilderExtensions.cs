using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Extension methods for injecting AI tools into an <see cref="ChatClientBuilder"/> pipeline.
/// </summary>
public static class ChatClientBuilderExtensions
{
    /// <summary>
    /// Injects all <see cref="AITool"/> services registered in DI into every request's
    /// <see cref="ChatOptions.Tools"/>. Tools are resolved once at build time.
    /// </summary>
    /// <remarks>
    /// Place this after <c>UseFunctionInvocation()</c> in the pipeline so the
    /// function-invoking client sees the tools on each request.
    /// <code>
    /// var client = chatClient.AsBuilder()
    ///     .UseFunctionInvocation()
    ///     .UseTools()
    ///     .Build(serviceProvider);
    /// </code>
    /// </remarks>
    public static ChatClientBuilder UseTools(this ChatClientBuilder builder)
    {
        return builder.Use((innerClient, sp) =>
        {
            var tools = sp.GetServices<AITool>().ToList();
            return new ToolInjectingChatClient(innerClient, tools);
        });
    }

    /// <summary>
    /// Injects an explicit set of <see cref="AITool"/> instances into every request's
    /// <see cref="ChatOptions.Tools"/>. Useful with on-demand tool contexts.
    /// </summary>
    /// <remarks>
    /// <code>
    /// var tools = CatalogTools.Default.GetTools(sp);
    /// var client = chatClient.AsBuilder()
    ///     .UseFunctionInvocation()
    ///     .UseTools(tools)
    ///     .Build(serviceProvider);
    /// </code>
    /// </remarks>
    public static ChatClientBuilder UseTools(this ChatClientBuilder builder, IEnumerable<AITool> tools)
    {
        var toolList = tools?.ToList() ?? throw new ArgumentNullException(nameof(tools));
        return builder.Use(innerClient => new ToolInjectingChatClient(innerClient, toolList));
    }

    private sealed class ToolInjectingChatClient(IChatClient innerClient, IReadOnlyList<AITool> tools)
        : DelegatingChatClient(innerClient)
    {
        public override Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return base.GetResponseAsync(messages, MergeTools(options), cancellationToken);
        }

        public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return base.GetStreamingResponseAsync(messages, MergeTools(options), cancellationToken);
        }

        private ChatOptions MergeTools(ChatOptions? options)
        {
            options = options is not null ? options.Clone() : new ChatOptions();
            options.Tools ??= [];

            foreach (var tool in tools)
            {
                if (!options.Tools.Contains(tool))
                    options.Tools.Add(tool);
            }

            return options;
        }
    }
}

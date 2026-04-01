using Microsoft.Extensions.AI;

namespace MauiSampleApp;

/// <summary>
/// A no-op IChatClient used when AI credentials are not configured.
/// Allows the app to start without crashing, but returns a helpful error message.
/// </summary>
public sealed class NoOpChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("NoOp");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ChatResponse(
            [new ChatMessage(ChatRole.Assistant, "AI is not configured. Please set up user secrets with AI:ApiKey, AI:Endpoint, and AI:DeploymentName.")])
        {
            ModelId = "no-op"
        };
        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GetStreamingResponseCore();

        static async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseCore()
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "AI is not configured.");
            await Task.CompletedTask;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

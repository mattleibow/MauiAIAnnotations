using Microsoft.Extensions.AI;

namespace MauiSampleApp.Core.Tests;

/// <summary>
/// A simple IChatClient for testing that returns a valid species profile JSON.
/// </summary>
public sealed class FakeSpeciesChatClient : IChatClient
{
    public ChatClientMetadata Metadata { get; } = new("FakeSpecies");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.LastOrDefault()?.Text ?? "";
        var name = "Unknown";
        var startIdx = lastMessage.IndexOf('"');
        var endIdx = lastMessage.IndexOf('"', startIdx + 1);
        if (startIdx >= 0 && endIdx > startIdx)
        {
            name = lastMessage[(startIdx + 1)..endIdx];
        }

        var capitalName = char.ToUpper(name[0]) + name[1..];
        var json = $$"""
            {
                "CommonName": "{{capitalName}}",
                "ScientificName": "{{capitalName}} testicus",
                "WateringFrequencyDays": 5,
                "SunlightNeeds": "Full",
                "FrostTolerant": false,
                "Notes": "Test notes for {{name}}."
            }
            """;

        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, json)]));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}

using MauiAIAnnotations.Maui.ViewModels;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.ViewModels;

public class GardenChatViewModel : ChatViewModel
{
    public GardenChatViewModel(IEnumerable<AITool> tools, IChatClient chatClient)
        : base(tools, chatClient)
    {
        AdditionalTools.Add(AIFunctionFactory.Create(
            () => new
            {
                MessageCount = Messages.Count,
                UserMessages = Messages.Count(m => m.Role == "User"),
                AssistantMessages = Messages.Count(m => m.Role == "Assistant"),
                LastUserMessage = Messages.Where(m => m.Role == "User" && m.Content is TextContent)
                    .Select(m => ((TextContent)m.Content).Text)
                    .LastOrDefault(),
            },
            "get_chat_context",
            "Gets context about the current conversation including message count and last user message."));
    }
}

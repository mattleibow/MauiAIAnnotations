using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.ViewModels;

/// <summary>
/// Back-compat shim for older samples and apps. Prefer <see cref="ChatSession"/> for new code.
/// </summary>
public class ChatViewModel : ChatSession
{
    public ChatViewModel(
        IEnumerable<AITool> tools,
        IChatClient chatClient)
        : base(tools, chatClient)
    {
    }
}

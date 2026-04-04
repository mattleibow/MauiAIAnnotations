namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Describes how a piece of chat content should be presented in the UI.
/// </summary>
public enum ContentRole
{
    User,
    Assistant,
    Tool,
    Approval,
    Error,
}

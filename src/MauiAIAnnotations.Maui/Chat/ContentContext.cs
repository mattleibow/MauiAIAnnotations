using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Wraps an <see cref="AIContent"/> item with its role for display in a chat UI.
/// Provides computed properties for direct XAML binding with compiled bindings.
/// </summary>
public partial class ContentContext : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Text))]
    [NotifyPropertyChangedFor(nameof(FunctionName))]
    [NotifyPropertyChangedFor(nameof(ErrorMessage))]
    [NotifyPropertyChangedFor(nameof(ResultText))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    public partial AIContent Content { get; set; }

    public string Role { get; }

    public ContentContext(AIContent content, string role)
    {
        Content = content;
        Role = role;
    }

    // Convenience properties for compiled binding in content views

    /// <summary>Text for TextContent messages (user and assistant).</summary>
    public string? Text => (Content as TextContent)?.Text;

    /// <summary>Function name for FunctionCallContent messages.</summary>
    public string? FunctionName => (Content as FunctionCallContent)?.Name;

    /// <summary>Error message for ErrorContent messages.</summary>
    public string? ErrorMessage => (Content as ErrorContent)?.Message;

    /// <summary>Stringified result for FunctionResultContent messages.</summary>
    public string? ResultText => (Content as FunctionResultContent)?.Result?.ToString();

    /// <summary>Fallback display text for any content type.</summary>
    public string? DisplayText => Content?.ToString();
}

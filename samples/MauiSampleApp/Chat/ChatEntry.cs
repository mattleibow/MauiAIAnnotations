using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiSampleApp.Chat;

/// <summary>
/// Type discriminator for chat entries, used by the template selector
/// to pick the appropriate visual template.
/// </summary>
public enum ChatEntryType
{
    UserText,
    AssistantText,
    ToolCall,
    ToolResult,
    Error
}

/// <summary>
/// Represents a single entry in the chat conversation. Implements INotifyPropertyChanged
/// so that streaming assistant responses can update the Content in-place and the UI
/// will refresh automatically.
/// </summary>
public class ChatEntry : INotifyPropertyChanged
{
    public required ChatEntryType Type { get; init; }

    private string _content = "";
    public required string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
    }

    public string? ToolName { get; init; }

    public string? ToolArgs { get; init; }

    /// <summary>
    /// Display-friendly role label derived from Type.
    /// </summary>
    public string RoleLabel => Type switch
    {
        ChatEntryType.UserText => "User",
        ChatEntryType.AssistantText => "Assistant",
        ChatEntryType.ToolCall => "Tool Call",
        ChatEntryType.ToolResult => "Tool Result",
        ChatEntryType.Error => "Error",
        _ => "Unknown"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

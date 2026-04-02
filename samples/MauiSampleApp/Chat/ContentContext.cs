using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

/// <summary>
/// Wraps an <see cref="AIContent"/> item with role information for display.
/// Implements INotifyPropertyChanged for streaming text updates.
/// </summary>
public class ContentContext : INotifyPropertyChanged
{
    private AIContent _content;

    public ContentContext(AIContent content, string role)
    {
        _content = content;
        Role = role;
    }

    public AIContent Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayText)); }
    }

    public string Role { get; }

    /// <summary>
    /// Convenience property for text display (used in templates).
    /// </summary>
    public string DisplayText => Content switch
    {
        TextContent text => text.Text ?? "",
        FunctionCallContent call => $"Calling {call.Name}...",
        FunctionResultContent result => result.Result?.ToString() ?? "",
        ErrorContent error => error.Message ?? "Unknown error",
        _ => Content.ToString() ?? ""
    };

    /// <summary>
    /// For FunctionResultContent that contains a Plant, returns the deserialized Plant.
    /// Used by PlantResultMapping's DataTemplate to bind to PlantCardView.
    /// </summary>
    public MauiSampleApp.Core.Models.Plant? PlantResult =>
        Content is FunctionResultContent result
            ? PlantResultMapping.TryGetPlant(result)
            : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

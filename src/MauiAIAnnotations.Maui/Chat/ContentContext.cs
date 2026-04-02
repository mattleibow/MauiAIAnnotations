using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

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
        set { _content = value; OnPropertyChanged(); }
    }

    public string Role { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

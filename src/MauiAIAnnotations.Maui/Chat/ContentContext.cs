using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class ContentContext : ObservableObject
{
    [ObservableProperty]
    public partial AIContent Content { get; set; }

    public string Role { get; }

    public ContentContext(AIContent content, string role)
    {
        Content = content;
        Role = role;
    }
}

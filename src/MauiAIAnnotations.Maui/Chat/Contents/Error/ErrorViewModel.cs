using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class ErrorViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is ErrorContent error)
        {
            Message = error.Message ?? "Unknown error";
        }
    }
}

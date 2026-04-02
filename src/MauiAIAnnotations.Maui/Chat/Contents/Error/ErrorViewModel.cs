using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class ErrorViewModel : INotifyPropertyChanged
{
    public string Message { get; private set; } = "";

    public void SetContext(ContentContext context)
    {
        if (context.Content is ErrorContent error)
        {
            Message = error.Message ?? "Unknown error";
            OnPropertyChanged(nameof(Message));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

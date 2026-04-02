using System.ComponentModel;

namespace MauiAIAnnotations.Maui.Chat;

public class DefaultContentViewModel : INotifyPropertyChanged
{
    public string DisplayText { get; private set; } = "";

    public void SetContext(ContentContext context)
    {
        DisplayText = context.Content?.ToString() ?? "";
        OnPropertyChanged(nameof(DisplayText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

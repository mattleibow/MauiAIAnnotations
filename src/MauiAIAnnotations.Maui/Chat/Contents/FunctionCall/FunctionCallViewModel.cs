using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionCallViewModel : INotifyPropertyChanged
{
    public string FunctionName { get; private set; } = "";
    public string Arguments { get; private set; } = "";

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionCallContent call)
        {
            FunctionName = call.Name;
            Arguments = call.Arguments is not null
                ? string.Join(", ", call.Arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "";
            OnPropertyChanged(nameof(FunctionName));
            OnPropertyChanged(nameof(Arguments));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

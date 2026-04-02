using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class FunctionResultViewModel : INotifyPropertyChanged
{
    public string ResultText { get; private set; } = "";

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            ResultText = result.Result?.ToString() ?? "";
            OnPropertyChanged(nameof(ResultText));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

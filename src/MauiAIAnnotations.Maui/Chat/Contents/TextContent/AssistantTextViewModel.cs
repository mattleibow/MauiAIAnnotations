using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public class AssistantTextViewModel : INotifyPropertyChanged
{
    private string _text = "";

    public string Text
    {
        get => _text;
        private set { _text = value; OnPropertyChanged(nameof(Text)); }
    }

    public void SetContext(ContentContext context)
    {
        if (context.Content is TextContent text)
        {
            Text = text.Text ?? "";
        }

        context.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ContentContext.Content) && context.Content is TextContent t)
                Text = t.Text ?? "";
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

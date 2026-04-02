using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class AssistantTextViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; }

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
}

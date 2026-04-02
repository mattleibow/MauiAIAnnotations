using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiAIAnnotations.Maui.Chat;

public partial class DefaultContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string DisplayText { get; set; }

    public void SetContext(ContentContext context)
    {
        DisplayText = context.Content?.ToString() ?? "";
    }
}

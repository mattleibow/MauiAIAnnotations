using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class FunctionResultViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string ResultText { get; set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            ResultText = result.Result?.ToString() ?? "";
        }
    }
}

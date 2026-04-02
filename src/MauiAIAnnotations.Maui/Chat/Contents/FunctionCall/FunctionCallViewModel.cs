using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations.Maui.Chat;

public partial class FunctionCallViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string FunctionName { get; set; }

    [ObservableProperty]
    public partial string Arguments { get; set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is FunctionCallContent call)
        {
            FunctionName = call.Name;
            Arguments = call.Arguments is not null
                ? string.Join(", ", call.Arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"))
                : "";
        }
    }
}

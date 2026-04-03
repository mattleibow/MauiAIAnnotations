using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Nickname { get; set; }

    [ObservableProperty]
    public partial string Species { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; }

    [ObservableProperty]
    public partial bool IsIndoor { get; set; }

    public ToolApprovalRequestContent? Request { get; private set; }

    public void SetContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        Request = approval;
        var args = fc.Arguments;
        if (args is null) return;

        Nickname = args.TryGetValue("nickname", out var n) ? n?.ToString() ?? "" : "";
        Species = args.TryGetValue("species", out var s) ? s?.ToString() ?? "" : "";
        Location = args.TryGetValue("location", out var l) ? l?.ToString() ?? "" : "";
        IsIndoor = args.TryGetValue("isIndoor", out var i) && i is true;
    }

    /// <summary>Builds modified arguments from the user's edits.</summary>
    public IDictionary<string, object?> BuildArguments() => new Dictionary<string, object?>
    {
        ["nickname"] = Nickname,
        ["species"] = Species,
        ["location"] = Location,
        ["isIndoor"] = IsIndoor,
    };
}

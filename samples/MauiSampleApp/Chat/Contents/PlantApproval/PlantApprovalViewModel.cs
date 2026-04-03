using CommunityToolkit.Mvvm.ComponentModel;
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

    public void LoadFrom(IDictionary<string, object?>? args)
    {
        if (args is null) return;
        Nickname = args.TryGetValue("nickname", out var n) ? n?.ToString() ?? "" : "";
        Species = args.TryGetValue("species", out var s) ? s?.ToString() ?? "" : "";
        Location = args.TryGetValue("location", out var l) ? l?.ToString() ?? "" : "";
        IsIndoor = args.TryGetValue("isIndoor", out var i) && i is true;
    }

    /// <summary>Writes the current values back to the FunctionCallContent.Arguments.</summary>
    public void WriteTo(FunctionCallContent fc)
    {
        fc.Arguments = new Dictionary<string, object?>
        {
            ["nickname"] = Nickname,
            ["species"] = Species,
            ["location"] = Location,
            ["isIndoor"] = IsIndoor,
        };
    }
}

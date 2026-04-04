using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalViewModel : ObservableObject, IContentContextAware
{
    private IDictionary<string, object?>? _args;

    [ObservableProperty]
    public partial string Nickname { get; set; }

    [ObservableProperty]
    public partial string Species { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; }

    [ObservableProperty]
    public partial bool IsIndoor { get; set; }

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        _args = fc.Arguments;
        if (_args is null) return;

        // MEAI sends structured args as: {"request": <JsonElement object>}
        if (_args.TryGetValue("request", out var reqObj) && reqObj is JsonElement json &&
            json.ValueKind == JsonValueKind.Object)
        {
            Nickname = json.TryGetProperty("nickname", out var n) ? n.GetString() ?? "" : "";
            Species = json.TryGetProperty("species", out var s) ? s.GetString() ?? "" : "";
            Location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "";
            IsIndoor = json.TryGetProperty("isIndoor", out var i) && i.ValueKind == JsonValueKind.True;
        }
    }

    partial void OnNicknameChanged(string value) => WriteBack();
    partial void OnSpeciesChanged(string value) => WriteBack();
    partial void OnLocationChanged(string value) => WriteBack();
    partial void OnIsIndoorChanged(bool value) => WriteBack();

    private void WriteBack()
    {
        if (_args is null) return;

        // Rebuild the request dict from current VM state (preserves the param name "request")
        _args["request"] = new Dictionary<string, object?>
        {
            ["nickname"] = Nickname,
            ["species"] = Species,
            ["location"] = Location,
            ["isIndoor"] = IsIndoor,
        };
    }
}

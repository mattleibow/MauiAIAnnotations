using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalViewModel : ObservableObject, IContentContextAware
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NicknameDisplay))]
    public partial string Nickname { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeciesDisplay))]
    public partial string Species { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocationDisplay))]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndoorDisplay))]
    public partial bool IsIndoor { get; set; }

    public string NicknameDisplay => FormatValue(Nickname);
    public string SpeciesDisplay => FormatValue(Species);
    public string LocationDisplay => FormatValue(Location);
    public string IndoorDisplay => IsIndoor ? "Yes" : "No";

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        var args = fc.Arguments;
        if (args is null) return;

        // MEAI sends structured args as: {"request": <JsonElement object>}
        if (args.TryGetValue("request", out var reqObj) && reqObj is JsonElement json &&
            json.ValueKind == JsonValueKind.Object)
        {
            Nickname = json.TryGetProperty("nickname", out var n) ? n.GetString() ?? "" : "";
            Species = json.TryGetProperty("species", out var s) ? s.GetString() ?? "" : "";
            Location = json.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "";
            IsIndoor = json.TryGetProperty("isIndoor", out var i) && i.ValueKind == JsonValueKind.True;
        }
    }

    private static string FormatValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not provided" : value;
}

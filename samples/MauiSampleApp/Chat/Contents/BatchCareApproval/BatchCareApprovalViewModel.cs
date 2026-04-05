using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class CareItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string EventType { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    public CareItemViewModel(string eventType, string notes = "")
    {
        EventType = eventType;
        Notes = notes;
    }
}

public partial class BatchCareApprovalViewModel : ObservableObject, IContentContextAware
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlantNicknameDisplay))]
    public partial string PlantNickname { get; set; } = string.Empty;

    public ObservableCollection<CareItemViewModel> CareItems { get; } = [];

    public string PlantNicknameDisplay => string.IsNullOrWhiteSpace(PlantNickname) ? "Unknown plant" : PlantNickname;

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        var args = fc.Arguments;
        if (args is null) return;

        PlantNickname = args.TryGetValue("plantNickname", out var n) && n is JsonElement nj
            ? nj.GetString() ?? ""
            : n?.ToString() ?? "";

        CareItems.Clear();
        if (args.TryGetValue("careEvents", out var eventsObj) && eventsObj is JsonElement json &&
            json.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in json.EnumerateArray())
            {
                if (e.ValueKind == JsonValueKind.Object)
                {
                    CareItems.Add(new CareItemViewModel(
                        e.TryGetProperty("eventType", out var et) ? et.GetString() ?? "" : "",
                        e.TryGetProperty("notes", out var nt) ? nt.GetString() ?? "" : ""));
                }
                else
                {
                    CareItems.Add(new CareItemViewModel(e.GetString() ?? ""));
                }
            }
        }
    }
}

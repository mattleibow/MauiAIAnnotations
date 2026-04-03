using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;

namespace MauiSampleApp.Chat;

public partial class CareItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string EventType { get; set; }

    [ObservableProperty]
    public partial string Notes { get; set; }

    public CareItemViewModel(string eventType, string notes = "")
    {
        EventType = eventType;
        Notes = notes;
        IsSelected = true;
    }
}

public partial class BatchCareApprovalViewModel : ObservableObject, IContentContextAware
{
    private IDictionary<string, object?>? _args;

    [ObservableProperty]
    public partial string PlantNickname { get; set; }

    public ObservableCollection<CareItemViewModel> CareItems { get; } = [];

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        _args = fc.Arguments;
        if (_args is null) return;

        PlantNickname = _args.TryGetValue("plantNickname", out var n) && n is JsonElement nj
            ? nj.GetString() ?? ""
            : n?.ToString() ?? "";

        CareItems.Clear();
        if (_args.TryGetValue("careEvents", out var eventsObj) && eventsObj is JsonElement json &&
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

        // Write back when items change
        foreach (var item in CareItems)
            item.PropertyChanged += (_, _) => WriteBack();
    }

    private void WriteBack()
    {
        if (_args is null) return;
        _args["careEvents"] = CareItems.Where(c => c.IsSelected)
            .Select(c => (object)new Dictionary<string, object?>
            {
                ["eventType"] = c.EventType,
                ["notes"] = c.Notes
            })
            .ToList();
    }
}

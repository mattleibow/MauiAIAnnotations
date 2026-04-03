using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiSampleApp.Chat;

public partial class CareItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial string EventType { get; set; }

    public CareItemViewModel(string eventType)
    {
        EventType = eventType;
        IsSelected = true;
    }
}

public partial class BatchCareApprovalViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string PlantNickname { get; set; }

    public System.Collections.ObjectModel.ObservableCollection<CareItemViewModel> CareItems { get; } = [];

    public void LoadFromArguments(IDictionary<string, object?>? args)
    {
        if (args is null) return;

        PlantNickname = args.TryGetValue("plantNickname", out var n) ? n?.ToString() ?? "" : "";

        CareItems.Clear();
        if (args.TryGetValue("eventTypes", out var eventsObj) && eventsObj is IEnumerable<object> events)
        {
            foreach (var e in events)
                CareItems.Add(new CareItemViewModel(e?.ToString() ?? ""));
        }
        else if (eventsObj is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var e in json.EnumerateArray())
                CareItems.Add(new CareItemViewModel(e.GetString() ?? ""));
        }
    }

    public IDictionary<string, object?> BuildArguments()
    {
        var selected = CareItems.Where(c => c.IsSelected).Select(c => c.EventType).ToList();
        return new Dictionary<string, object?>
        {
            ["plantNickname"] = PlantNickname,
            ["eventTypes"] = selected,
        };
    }
}

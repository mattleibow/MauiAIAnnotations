using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI.Maui.Chat;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Models;

namespace MauiSampleApp.Chat;

public partial class CareItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string EventType { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNotes))]
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

        PlantNickname = ReadStringArgument(args, "plantNickname");

        CareItems.Clear();

        if (args.TryGetValue("careEvents", out var eventsObj) && eventsObj is not null)
        {
            foreach (var item in ParseCareItems(eventsObj))
            {
                CareItems.Add(item);
            }
        }

        if (CareItems.Count == 0)
        {
            CareItems.Add(new CareItemViewModel(string.Empty));
        }
    }

    public ToolApprovalResponseContent CreateApprovalResponse(ToolApprovalRequestContent request, bool approved)
    {
        if (!approved || request.ToolCall is not FunctionCallContent functionCall)
        {
            return request.CreateResponse(approved, approved ? null : "User rejected");
        }

        var updatedArgs = functionCall.Arguments?.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal)
            ?? new Dictionary<string, object?>(StringComparer.Ordinal);

        updatedArgs["plantNickname"] = PlantNickname.Trim();
        updatedArgs["careEvents"] = CareItems
            .Select(static item => new CareEventRequest
            {
                EventType = item.EventType.Trim(),
                Notes = item.Notes.Trim(),
            })
            .Where(static item => !string.IsNullOrWhiteSpace(item.EventType) || !string.IsNullOrWhiteSpace(item.Notes))
            .ToList();

        var editedCall = new FunctionCallContent(functionCall.CallId, functionCall.Name, updatedArgs);
        return new ToolApprovalResponseContent(request.RequestId, approved: true, editedCall);
    }

    [RelayCommand]
    private void AddCareItem()
    {
        CareItems.Add(new CareItemViewModel(string.Empty));
    }

    [RelayCommand]
    private void RemoveCareItem(CareItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        CareItems.Remove(item);
        if (CareItems.Count == 0)
        {
            CareItems.Add(new CareItemViewModel(string.Empty));
        }
    }

    private static string ReadStringArgument(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var value) || value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static IEnumerable<CareItemViewModel> ParseCareItems(object eventsObj)
    {
        if (eventsObj is JsonElement json && json.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in json.EnumerateArray())
            {
                yield return element.ValueKind == JsonValueKind.Object
                    ? new CareItemViewModel(
                        element.TryGetProperty("eventType", out var eventType) ? eventType.GetString() ?? string.Empty : string.Empty,
                        element.TryGetProperty("notes", out var notes) ? notes.GetString() ?? string.Empty : string.Empty)
                    : new CareItemViewModel(element.ToString());
            }

            yield break;
        }

        if (eventsObj is IEnumerable<CareEventRequest> typedEvents)
        {
            foreach (var item in typedEvents)
            {
                yield return new CareItemViewModel(item.EventType, item.Notes);
            }

            yield break;
        }

        if (eventsObj is IEnumerable<object?> objectEvents)
        {
            foreach (var item in objectEvents)
            {
                if (item is CareEventRequest typed)
                {
                    yield return new CareItemViewModel(typed.EventType, typed.Notes);
                }
                else if (item is IDictionary<string, object?> dictionary)
                {
                    yield return new CareItemViewModel(
                        dictionary.TryGetValue("eventType", out var eventType) ? eventType?.ToString() ?? string.Empty : string.Empty,
                        dictionary.TryGetValue("notes", out var notes) ? notes?.ToString() ?? string.Empty : string.Empty);
                }
                else if (item is not null)
                {
                    yield return new CareItemViewModel(item.ToString() ?? string.Empty);
                }
            }
        }
    }
}

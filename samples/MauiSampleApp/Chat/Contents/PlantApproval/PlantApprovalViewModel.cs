using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI.Maui.Chat;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Models;

namespace MauiSampleApp.Chat;

public partial class PlantApprovalViewModel : ObservableObject, IContentContextAware
{
    [ObservableProperty]
    public partial string Nickname { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Species { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndoorDisplay))]
    public partial bool IsIndoor { get; set; }

    public string IndoorDisplay => IsIndoor ? "Yes" : "No";

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is not ToolApprovalRequestContent approval ||
            approval.ToolCall is not FunctionCallContent fc)
            return;

        var args = fc.Arguments;
        if (args is null) return;

        if (!args.TryGetValue("request", out var requestObject) || requestObject is null)
        {
            return;
        }

        switch (requestObject)
        {
            case JsonElement json when json.ValueKind == JsonValueKind.Object:
                Nickname = json.TryGetProperty("nickname", out var nickname) ? nickname.GetString() ?? string.Empty : string.Empty;
                Species = json.TryGetProperty("species", out var species) ? species.GetString() ?? string.Empty : string.Empty;
                Location = json.TryGetProperty("location", out var location) ? location.GetString() ?? string.Empty : string.Empty;
                IsIndoor = json.TryGetProperty("isIndoor", out var indoor) && indoor.ValueKind == JsonValueKind.True;
                break;

            case NewPlantRequest typedRequest:
                Nickname = typedRequest.Nickname;
                Species = typedRequest.Species;
                Location = typedRequest.Location;
                IsIndoor = typedRequest.IsIndoor;
                break;

            case IDictionary<string, object?> dictionary:
                Nickname = dictionary.TryGetValue("nickname", out var nick) ? nick?.ToString() ?? string.Empty : string.Empty;
                Species = dictionary.TryGetValue("species", out var spec) ? spec?.ToString() ?? string.Empty : string.Empty;
                Location = dictionary.TryGetValue("location", out var loc) ? loc?.ToString() ?? string.Empty : string.Empty;
                IsIndoor = dictionary.TryGetValue("isIndoor", out var indoorValue) && bool.TryParse(indoorValue?.ToString(), out var indoorFlag) && indoorFlag;
                break;
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

        updatedArgs["request"] = new NewPlantRequest
        {
            Nickname = Nickname.Trim(),
            Species = Species.Trim(),
            Location = Location.Trim(),
            IsIndoor = IsIndoor,
        };

        var editedCall = new FunctionCallContent(functionCall.CallId, functionCall.Name, updatedArgs);
        return new ToolApprovalResponseContent(request.RequestId, approved: true, editedCall);
    }
}

# MauiAIAnnotations

A .NET 10 library that makes it easy to expose service methods as AI tools and build chat-powered MAUI apps with a clean `Microsoft.Extensions.AI` pipeline.

| Windows | Android |
| --- | --- |
| <img src="images/home-page.png" alt="Garden Helper App on Windows" width="300" /> | <img src="images/home-page-android.png" alt="Garden Helper App on Android" width="300" /> |

## Start here

Pick the path that matches your goal:

| Path | Best for | Guide |
| --- | --- | --- |
| **Quick start** | A working MAUI chat + function-calling flow with the fewest steps | [Getting Started](getting-started.md) |
| **Approval flow** | Approve/reject sensitive tool calls before they run | [Human-in-the-Loop Approval](human-in-the-loop.md) |
| **Rich tool UI** | Show tool results as cards, widgets, or other custom views | [Custom Tool Rendering](tool-rendering.md) |

### What those paths look like

**Quick start - chat + tool calls**

| Windows | Android |
| --- | --- |
| <img src="images/chat-sidebar.png" alt="Open Chat Panel on Windows" width="300" /> | <img src="images/chat-sidebar-android.png" alt="Open Chat Panel on Android" width="300" /> |

**Approval flow - review before execution**

| Windows | Android |
| --- | --- |
| <img src="images/approval-approved.png" alt="Approval Card on Windows" width="300" /> | <img src="images/approval-approved-android.png" alt="Approval Card on Android" width="300" /> |

**Rich tool UI - custom tool result cards**

| Windows | Android |
| --- | --- |
| <img src="images/plant-card.png" alt="Plant Card in Chat on Windows" width="300" /> | <img src="images/plant-card-android.png" alt="Plant Card in Chat on Android" width="300" /> |

## Quick start in 3 steps

### 1. Annotate service methods

```csharp
public class PlantDataService
{
    [ExportAIFunction("get_plants", Description = "Gets all plants the user has registered.")]
    public async Task<List<Plant>> GetPlantsAsync() => ...;

    [ExportAIFunction(
        "add_plant",
        Description = "Adds a new plant to the garden.",
        ApprovalRequired = true)]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name for the plant")] string nickname,
        [Description("The species name")] string species) => ...;
}
```

### 2. Register tools and the chat pipeline

```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools(typeof(PlantDataService).Assembly);
builder.Services.AddAIChat();

builder.Services.AddSingleton<IChatClient>(provider =>
{
    return openAiChatClient
        .AsIChatClient()
        .AsBuilder()
        .UseMauiToolApproval()
        .UseFunctionInvocation()
        .Build(provider);
});
```

### 3. Add `ChatPanelControl` to your page

```xml
<maui:ChatPanelControl ChatVM="{Binding ChatViewModel}">
    <maui:ChatPanelControl.ContentTemplates>
        <mauiChat:TextContentTemplate Role="User" />
        <mauiChat:TextContentTemplate Role="Assistant" />
        <mauiChat:FunctionCallTemplate />
        <mauiChat:FunctionResultTemplate />
        <mauiChat:ToolApprovalTemplate />
        <mauiChat:ErrorContentTemplate />
        <mauiChat:DefaultContentTemplate />
    </maui:ChatPanelControl.ContentTemplates>
</maui:ChatPanelControl>
```

Keep `UseMauiToolApproval()` before `UseFunctionInvocation()` so approval-required tools can pause and resume cleanly.

## Next paths

- [Getting Started](getting-started.md) - the full first-run walkthrough.
- [Human-in-the-Loop Approval](human-in-the-loop.md) - built-in approval cards plus optional custom review views.
- [Custom Tool Rendering](tool-rendering.md) - result cards and tool-specific content templates.

## Packages

| Package | Description |
| --- | --- |
| **MauiAIAnnotations** | Attribute-based AI tool discovery. Decorate methods with `[ExportAIFunction]` and call `AddAITools()` to register them in DI. |
| **MauiAIAnnotations.Maui** | Reusable MAUI chat UI, content template system, and human-in-the-loop approval dialogs. |

## Requirements

- .NET 10.0+
- `Microsoft.Extensions.AI` 10.4.1+
- `Microsoft.Extensions.DependencyInjection` 10.0.0+

## License

See [LICENSE](../LICENSE) for details.

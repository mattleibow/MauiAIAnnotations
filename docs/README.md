# MauiAIAnnotations

A .NET 10 library that makes it easy to expose service methods as AI tools and build chat-powered MAUI apps with a clean `Microsoft.Extensions.AI` pipeline.

![Garden Helper App](images/home-page.png)

## Start here

Pick the path that matches your goal:

| Path | Best for | Guide |
| --- | --- | --- |
| **Quick start** | A working MAUI chat + function-calling flow with the fewest steps | [Getting Started](getting-started.md) |
| **Approval flow** | Approve/reject sensitive tool calls before they run | [Human-in-the-Loop Approval](human-in-the-loop.md) |
| **Rich tool UI** | Show tool results as cards, widgets, or other custom views | [Custom Tool Rendering](tool-rendering.md) |

### What those paths look like

**Quick start - chat + tool calls**

![Chat Sidebar](images/chat-sidebar.png)

**Approval flow - review before execution**

![Approval Card](images/approval-approved.png)

**Rich tool UI - custom tool result cards**

![Plant Card in Chat](images/plant-card.png)

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

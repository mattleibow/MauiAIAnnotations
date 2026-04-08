# MauiAIAnnotations

Turn regular .NET services into AI-callable tools and host them in a reusable chat experience without hand-writing JSON schemas or tool adapters.

`MauiAIAnnotations` handles **reflection-based tool discovery**, schema generation, and the **headless `ChatSession` engine**.  
`MauiAIAnnotations.Maui` adds the **thin MAUI chat panel, content templates, and approval UI** on top of that core session.

| Windows | Android |
| --- | --- |
| <img src="docs/images/home-page.png" alt="Garden Helper App on Windows" width="300" /> | <img src="docs/images/home-page-android.png" alt="Garden Helper App on Android" width="300" /> |

## Start here

Choose the path that matches what you need:

| Path | Best for | Guide |
| --- | --- | --- |
| **Quick start** | Add AI chat + function calling to an existing MAUI page fast | [Getting Started](docs/getting-started.md) |
| **Approval flow** | Require approve/reject before sensitive tools run | [Human-in-the-Loop Approval](docs/human-in-the-loop.md) |
| **Tool result views** | Start with the default result view, then replace it with cards or widgets when needed | [Custom Tool Rendering](docs/tool-rendering.md) |

### What each path looks like

**Quick start - chat + tool calls**

| Windows | Android |
| --- | --- |
| <img src="docs/images/chat-sidebar.png" alt="Open Chat Panel on Windows" width="300" /> | <img src="docs/images/chat-sidebar-android.png" alt="Open Chat Panel on Android" width="300" /> |

**Approval flow - review before execution**

| Windows | Android |
| --- | --- |
| <img src="docs/images/approval-approved.png" alt="Approval Card on Windows" width="300" /> | <img src="docs/images/approval-approved-android.png" alt="Approval Card on Android" width="300" /> |

**Rich tool UI - custom result cards**

| Windows | Android |
| --- | --- |
| <img src="docs/images/plant-card.png" alt="Plant Card on Windows" width="300" /> | <img src="docs/images/plant-card-android.png" alt="Plant Card on Android" width="300" /> |

## Quick start in 3 steps

### 1. Annotate your service methods

```csharp
using MauiAIAnnotations;
using System.ComponentModel;

public class PlantDataService
{
    [Description("Gets all plants the user has registered.")]
    [ExportAIFunction("get_plants")]
    public async Task<List<Plant>> GetPlantsAsync() => ...;

    [Description("Adds a new plant to the garden.")]
    [ExportAIFunction("add_plant", ApprovalRequired = true)]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name for the plant")] string nickname,
        [Description("The species name")] string species) => ...;
}
```

### 2. Register tools and the headless chat session

```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools(typeof(PlantDataService).Assembly);
builder.Services.AddChatSession(ServiceLifetime.Transient);

builder.Services.AddSingleton<IChatClient>(provider =>
{
    return openAiChatClient
        .AsIChatClient()
        .AsBuilder()
        .UseFunctionInvocation()
        .Build(provider);
});
```

### 3. Drop the chat panel onto your page

```xml
<maui:ChatPanelControl Session="{Binding ChatSession}">
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

`ChatSession` is the framework-facing conversation engine. `ChatPanelControl` just renders any `IChatSession` you give it through the `Session` property, so the MAUI layer stays UI-only instead of owning a framework-managed `BindingContext` or chat ViewModel.

## When you want more than the basics

- **Need a guided first integration?** Start with [Getting Started](docs/getting-started.md).
- **Need review-before-run for writes/deletes?** Use [Human-in-the-Loop Approval](docs/human-in-the-loop.md).
- **Need the default tool result view or a custom one?** Use [Custom Tool Rendering](docs/tool-rendering.md).
- **Need a working reference app?** See [`samples/MauiSampleApp`](samples/MauiSampleApp).

## Packages

| Package | Description |
| --- | --- |
| `MauiAIAnnotations` | Attribute-based AI tool discovery for `Microsoft.Extensions.AI` |
| `MauiAIAnnotations.Maui` | Reusable MAUI chat UI, content templates, and approval dialogs |

## Requirements

- .NET 10.0+
- `Microsoft.Extensions.AI` 10.4.1+
- `Microsoft.Extensions.DependencyInjection` 10.0.0+

## License

See [LICENSE](LICENSE) for details.

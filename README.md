# Microsoft.Extensions.AI Attributes / Chat / Maui

Turn regular .NET services into AI-callable tools and host them in a reusable MAUI chat experience — no hand-written JSON schemas, tool adapters, or runtime reflection.

`Microsoft.Extensions.AI.Attributes` handles **source-generated tool discovery** and DI registration.  
`Microsoft.Extensions.AI.Chat` adds the **headless `ChatSession` engine** and approval-aware conversation flow.  
`Microsoft.Extensions.AI.Maui` adds the **thin MAUI chat panel, content templates, and approval UI** on top.

| Windows | Android |
| --- | --- |
| <img src="docs/images/home-page.png" alt="Garden Helper App on Windows" width="300" /> | <img src="docs/images/home-page-android.png" alt="Garden Helper App on Android" width="300" /> |

## Quick start

### 1. Annotate your service methods

```csharp
using Microsoft.Extensions.AI.Attributes;
using System.ComponentModel;

public class PlantDataService
{
    [Description("Gets all plants the user has registered.")]
    [ExportAIFunction("get_plants")]
    public async Task<List<Plant>> GetPlantsAsync() => ...;

    [Description("Adds a new plant.")]
    [ExportAIFunction("add_plant", ApprovalRequired = true)]
    public async Task<Plant> AddPlantAsync(
        [Description("A friendly name")] string nickname,
        [Description("The species")] string species) => ...;
}
```

### 2. Define a tool context

```csharp
[AIToolSource(typeof(PlantDataService))]
[AIToolSource(typeof(SeasonsService))]
public partial class GardenTools : AIToolContext { }
```

The source generator emits the registration code at compile time — no reflection.

### 3. Register tools and chat

```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools<GardenTools>();
builder.Services.AddChatSession(ServiceLifetime.Transient);
```

### 4. Add the MAUI panel

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

## Packages

| Package | Description |
| --- | --- |
| `Microsoft.Extensions.AI.Attributes` | Source-generated AI tool discovery with `[ExportAIFunction]` and `AddAITools<T>()` |
| `Microsoft.Extensions.AI.Chat` | Headless `ChatSession`, transcript types, and approval-aware chat flow |
| `Microsoft.Extensions.AI.Maui` | Reusable MAUI chat UI, content templates, and approval dialogs |

## Sample apps

| App | Description |
| --- | --- |
| `MauiSampleApp` | Full garden helper with custom approval views, tool rendering, and DevFlow |
| `AnnotationsSampleApp` | Focused demo of annotations, tool contexts, and DI lifetimes (no Chat/Maui libs) |

See each `src/*/README.md` for library-specific usage documentation.

# Default and Custom Tool Result Rendering

Tool results already have a built-in **default view** in the chat. If that is enough, keep the stock templates and you are done. If you want something richer — a plant card, a weather widget, a product tile — add a **custom content template mapping** for the specific tool results you care about.

This guide covers both paths:

| Path | Use it when | What you do |
| --- | --- | --- |
| **Default view** | You want the fastest setup or a good debug-friendly fallback | Keep `FunctionResultTemplate` and `DefaultContentTemplate` in `ChatPanelControl` |
| **Custom view** | You want cards, widgets, or tool-specific UI | Add a `ContentTemplate` with a custom `ViewType` before the generic fallback |

## How to Use the Default Result View

The default result view is built in. You do **not** need a custom renderer just to show tool results in the chat.

### 1. Register the built-in templates

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

### 2. Return normal tool results

Your tool can return a string, a DTO, a list, or another normal .NET object:

```csharp
[Description("Gets all plants the user has registered.")]
[ExportAIFunction("get_plants")]
public async Task<List<Plant>> GetPlantsAsync() => await _repo.GetPlantsAsync();
```

The chat panel will show:

- the tool call via `FunctionCallTemplate`
- the returned value via `FunctionResultTemplate`
- a generic fallback via `DefaultContentTemplate` if nothing more specific matches

That is the right starting point for most apps. It is also the fallback you should keep even after adding custom views.

### Default view result

| Windows | Android |
| --- | --- |
| <img src="images/function-calls.png" alt="Default Function Result View on Windows" width="300" /> | <img src="images/function-calls-android.png" alt="Default Function Result View on Android" width="300" /> |

## How Content Templates Work

The chat UI uses a `ContentTemplateSelector` that holds an ordered list of `ContentTemplate` objects. For each chat item it iterates the list and picks the **first** mapping whose `When()` predicate returns `true`.

```
ContentTemplateSelector
  ├─ PlantResultTemplate      → PlantResultView   ← matches Plant results
  ├─ FunctionResultTemplate   → FunctionResultView ← catches all other results
  └─ DefaultContentTemplate   → DefaultContentView
```

**Order matters.** Specific mappings must appear before generic ones, otherwise the generic mapping matches first and your custom view is never used.

## How to Build a Custom View

Once the default result view is working, you can replace it for specific tools or result shapes.

### Step 1: Create a ContentTemplate

Subclass `ContentTemplate` and override `When()` to match the tool results you care about:

```csharp
using System.Text.Json;
using MauiAIAnnotations.Maui.Chat;
using Microsoft.Extensions.AI;
using MauiSampleApp.Core.Models;

public class PlantResultTemplate : ContentTemplate
{
    public override bool When(ContentContext context)
    {
        if (context.Content is not FunctionResultContent result)
            return false;

        return TryGetPlant(result) is not null;
    }

    public static Plant? TryGetPlant(FunctionResultContent result)
    {
        try
        {
            if (result.Result is Plant plant)
                return plant;

            if (result.Result is JsonElement json)
            {
                return JsonSerializer.Deserialize<Plant>(json.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
        }
        catch
        {
            // Not a Plant — fall through to the generic template
        }

        return null;
    }
}
```

The `TryGetPlant` helper handles both strongly-typed returns and JSON payloads, which is common when the AI serializes results.

### Step 2: Create a ViewModel (Optional)

Built-in library views bind directly to `ContentContext` — no library-owned ViewModel required. But when your app-specific view needs to **extract or transform** data from the AI response, a small local ViewModel keeps the view clean:

```csharp
public partial class PlantResultViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Plant? Plant { get; set; }

    public void ApplyContentContext(ContentContext context)
    {
        if (context.Content is FunctionResultContent result)
        {
            Plant = PlantResultTemplate.TryGetPlant(result);
        }
    }
}
```

### Step 3: Create the XAML View

The view can receive a `ContentContext` explicitly by deriving from `ContentContextView`. The code-behind exposes a strongly typed `ViewModel` property, so the XAML can bind explicitly without depending on the ambient `BindingContext`:

```xml
<maui:ContentContextView x:Class="MauiSampleApp.Chat.PlantResultView"
                         x:Name="This">
    <Grid Padding="0,4">
        <VerticalStackLayout BindingContext="{Binding ViewModel, Source={x:Reference This}}"
                             x:DataType="chat:PlantResultViewModel">
            <controls:PlantCardView BindingContext="{Binding Plant}"
                                    HorizontalOptions="Start"
                                    MaximumWidthRequest="320" />
        </VerticalStackLayout>
    </Grid>
</maui:ContentContextView>
```

```csharp
public partial class PlantResultView : ContentContextView
{
    private readonly PlantResultViewModel _vm = new();
    public PlantResultViewModel ViewModel => _vm;

    public PlantResultView() => InitializeComponent();

    protected override void RefreshFromContentContext()
    {
        if (ContentContext is not null)
            _vm.ApplyContentContext(ContentContext);
    }
}
```

The `PlantCardView` inside is a normal MAUI control that binds to `Plant` properties like `Nickname`, `Location`, and `IsIndoor`.

### Step 4: Register the Template

Add your mapping to the `ContentTemplates` list in the page XAML. Place it **before** the generic `FunctionResultTemplate`:

```xml
<maui:ChatPanelControl Session="{Binding ChatSession}">
    <maui:ChatPanelControl.ContentTemplates>
        <!-- ... other mappings ... -->
        <local:PlantResultTemplate ViewType="{x:Type local:PlantResultView}" />
        <mauiChat:FunctionResultTemplate ViewType="{x:Type mauiChat:FunctionResultView}" />
        <!-- ... remaining mappings ... -->
    </maui:ChatPanelControl.ContentTemplates>
</maui:ChatPanelControl>
```

If `PlantResultTemplate` appears after `FunctionResultTemplate`, the generic mapping matches every `FunctionResultContent` first and the plant card never shows.

### Custom view result

| Windows | Android |
| --- | --- |
| <img src="images/plant-card.png" alt="Plant Card in Chat on Windows" width="300" /> | <img src="images/plant-card-android.png" alt="Plant Card in Chat on Android" width="300" /> |

## When to Use Each Pattern

| Scenario | Example |
|---|---|
| Default view is enough | Quick start, internal tools, debugging raw output |
| Rich data cards | Plant info, weather, product details |
| Interactive elements | Buttons, links, rating controls |
| Custom visualizations | Charts, maps, progress indicators |
| Tool-specific filtering | Match by tool name in `When()` to only show the card for specific tools |

For even deeper customization — like intercepting a tool call **before** it executes and letting the user approve or reject it — see [Human-in-the-Loop Tool Approval](human-in-the-loop.md).

> **Full sample code:** See `samples/MauiSampleApp/Chat/Contents/PlantResult/` for the complete PlantCardView implementation.

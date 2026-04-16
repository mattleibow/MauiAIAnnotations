# Microsoft.Extensions.AI.Maui

Reusable MAUI chat UI that renders an `IChatSession` from `Microsoft.Extensions.AI.Chat`. Provides `ChatPanelControl`, a content template system, and built-in approval views.

## How It Works

### 1. Add the chat panel to a page

```xml
xmlns:maui="clr-namespace:Microsoft.Extensions.AI.Maui.Controls;assembly=Microsoft.Extensions.AI.Maui"
xmlns:mauiChat="clr-namespace:Microsoft.Extensions.AI.Maui.Chat;assembly=Microsoft.Extensions.AI.Maui"
```

```xml
<maui:ChatPanelControl Session="{Binding Source={x:Reference ThisPage}, Path=ChatSession}">
    <maui:ChatPanelControl.ContentTemplates>
        <mauiChat:TextContentTemplate Role="User" />
        <mauiChat:TextContentTemplate Role="Assistant" />
        <mauiChat:FunctionCallTemplate />
        <mauiChat:ToolApprovalTemplate />
        <mauiChat:FunctionResultTemplate />
        <mauiChat:ErrorContentTemplate />
        <mauiChat:DefaultContentTemplate />
    </maui:ChatPanelControl.ContentTemplates>
</maui:ChatPanelControl>
```

`ChatPanelControl` is a framework control — bind the `Session` property to your `ChatSession` instance. It manages its own input bar, busy indicator, and message list internally.

### 2. Content templates

The chat panel uses an ordered list of `ContentTemplate` objects to render each message. For each `ChatEntry`, the first template whose `When()` returns `true` is used.

**Order matters** — specific templates before generic ones.

Built-in templates:

| Template | Matches |
|---|---|
| `TextContentTemplate` | Text messages (filter by `Role`) |
| `FunctionCallTemplate` | Tool call indicators |
| `FunctionResultTemplate` | Tool results (generic) |
| `ToolApprovalTemplate` | Approval request cards (filter by `ToolName`) |
| `ErrorContentTemplate` | Error messages |
| `DefaultContentTemplate` | Catch-all fallback |

### 3. Custom tool-specific templates

Override rendering for specific tools by placing a custom template before the generic one:

```xml
<!-- Custom view for add_plant results -->
<local:PlantResultTemplate ToolName="add_plant"
    ViewType="{x:Type local:PlantResultView}" />

<!-- Custom approval view for add_plant -->
<mauiChat:ToolApprovalTemplate ToolName="add_plant"
    ViewType="{x:Type local:PlantApprovalView}" />

<!-- Generic fallbacks (must come after specific ones) -->
<mauiChat:ToolApprovalTemplate />
<mauiChat:FunctionResultTemplate />
```

### 4. Custom content views

Subclass `ContentContextView` to create a view that receives the `ContentContext`:

```csharp
public partial class PlantResultView : ContentContextView
{
    public PlantResultView() => InitializeComponent();

    protected override void RefreshFromContentContext()
    {
        // Extract data from ContentContext.Content
    }
}
```

Views bind directly to `ContentContext` computed properties (Text, FunctionName, ErrorMessage, ResultText). No intermediate ViewModels are needed for library views. App-specific views may use their own ViewModels by implementing `IContentContextAware`.

### 5. Custom approval views

For approval cards, implement `IToolApprovalResponseFactory` to return edited arguments:

```csharp
public ToolApprovalResponseContent CreateApprovalResponse(
    ToolApprovalRequestContent request, bool approved)
{
    var original = (FunctionCallContent)request.ToolCall;
    var edited = new FunctionCallContent(original.CallId, original.Name,
        new Dictionary<string, object?> { ["nickname"] = EditedNickname });
    return new ToolApprovalResponseContent(request.RequestId, approved, edited);
}
```

The built-in `ToolApprovalView` handles the Approve/Reject buttons and submission. Custom views only provide the review content.

## Key Types

| Type | Description |
|---|---|
| `ChatPanelControl` | Main chat UI control — bind `Session` to an `IChatSession` |
| `ContentTemplate` | Base class for content template matchers |
| `ContentTemplateSelector` | Picks the first matching template for each chat entry |
| `ContentContext` | Context object passed to views (session + entry) |
| `ContentContextView` | Base class for custom content views |
| `IContentContextAware` | Interface for ViewModels that receive `ContentContext` |
| `IToolApprovalResponseFactory` | Interface for views that return edited approval responses |

## Layout

`ChatPanelControl` can be placed anywhere: inline, in a bottom sheet, as a sidebar, or full-screen. It manages its own input area and auto-scrolling. The host page controls placement and sizing.

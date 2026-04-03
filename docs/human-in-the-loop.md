# Human-in-the-Loop Approval

## Overview

Some AI tool calls are too sensitive to auto-execute — adding, deleting, or modifying
data should require explicit user consent. The `[ExportAIFunction]` attribute supports
an `ApprovalRequired` flag that tells the system to pause and present an approval card
before the tool is invoked. The user can review the arguments, optionally edit them,
and then approve or reject the action.

## How It Works

1. **Mark the method** — Set `ApprovalRequired = true` on the `[ExportAIFunction]` attribute.
2. **Discovery wraps it** — When `AddAITools()` discovers the function, it wraps it in
   an `ApprovalRequiredAIFunction` (from `Microsoft.Extensions.AI`).
3. **Chat client yields an approval request** — `FunctionInvokingChatClient` recognises
   the wrapper and emits a `ToolApprovalRequestContent` instead of auto-invoking.
4. **ViewModel pauses the conversation** — `ChatViewModel` detects the approval request,
   pauses streaming, and surfaces the request in the chat UI.
5. **User reviews & decides** — The user sees an approval card where they can inspect
   (and optionally edit) the arguments, then tap **Approve** or **Reject**.
6. **Result flows back** — On approval the tool executes with the (possibly modified)
   arguments. On rejection the tool is skipped and the AI is informed.

## Step 1: Mark Sensitive Tools

Add `ApprovalRequired = true` to any tool that mutates data:

```csharp
[ExportAIFunction("add_plant", Description = "Adds a new plant.", ApprovalRequired = true)]
public async Task<Plant> AddPlantAsync(
    string name,
    string description,
    PlantType type) { ... }

[ExportAIFunction("remove_plant", Description = "Removes a plant.", ApprovalRequired = true)]
public async Task RemovePlantAsync(int plantId) { ... }
```

That's it — no changes to `MauiProgram.cs` are needed. The discovery pipeline handles
the wrapping automatically.

## Step 2: Register the Approval Template

The library ships with a generic `ToolApprovalView` that works for any tool. Register
it as a chat item template so the chat UI knows how to render approval requests:

```xml
<mauiChat:ToolApprovalMapping ViewType="{x:Type mauiChat:ToolApprovalView}" />
```

This displays a card with the tool name, a read-only summary of the arguments, and
**Approve** / **Reject** buttons.

![Approval Request](images/approval-request.png)

## Step 3: (Optional) Create a Custom Approval View

For a richer experience you can create tool-specific approval views that let the user
edit individual arguments before approving.

### 1. Create a custom mapping

Create a `PlantApprovalMapping` class that matches only the `add_plant` tool:

```csharp
public class PlantApprovalMapping : ToolApprovalMapping
{
    public override bool Matches(ToolApprovalRequestContent request)
        => request.FunctionCallContent.Name == "add_plant";
}
```

### 2. Create a view-model with editable properties

```csharp
public class PlantApprovalViewModel : ToolApprovalViewModel
{
    public string PlantName { get; set; }
    public string Description { get; set; }
    public PlantType Type { get; set; }

    public override void LoadFromRequest(ToolApprovalRequestContent request)
    {
        var args = request.FunctionCallContent.Arguments;
        PlantName = args["name"]?.ToString();
        Description = args["description"]?.ToString();
        // ...
    }

    public override IDictionary<string, object?> GetModifiedArguments()
        => new Dictionary<string, object?>
        {
            ["name"] = PlantName,
            ["description"] = Description,
            ["type"] = Type,
        };
}
```

### 3. Create the XAML view

```xml
<ContentView x:Class="MyApp.PlantApprovalView">
    <VerticalStackLayout Padding="12" Spacing="8">
        <Label Text="Add Plant — Review &amp; Approve" FontAttributes="Bold" />
        <Entry Text="{Binding PlantName}" Placeholder="Name" />
        <Entry Text="{Binding Description}" Placeholder="Description" />
        <HorizontalStackLayout Spacing="8">
            <Button Text="Approve" Command="{Binding ApproveCommand}" />
            <Button Text="Reject"  Command="{Binding RejectCommand}" />
        </HorizontalStackLayout>
    </VerticalStackLayout>
</ContentView>
```

### 4. Register before the generic mapping

Order matters — more specific mappings must come first:

```xml
<local:PlantApprovalMapping ViewType="{x:Type local:PlantApprovalView}" />
<mauiChat:ToolApprovalMapping ViewType="{x:Type mauiChat:ToolApprovalView}" />
```

## After Approval

When the user edits arguments and taps **Approve**, the modified values are forwarded
to the tool. In the screenshot below the user changed *"Sun Daisy"* to
*"Golden Daisy"* before approving — the tool received the updated name.

![After Approval](images/approval-approved.png)

## After Rejection

When the user taps **Reject**, the tool is **not** invoked. The AI receives a
rejection signal and acknowledges it in the conversation.

![After Rejection](images/approval-rejected.png)

## Key Points

- **`ApprovalRequired = true`** is the only change needed in your service code.
- **No `MauiProgram.cs` changes** — the discovery pipeline wraps the function
  automatically.
- **Custom approval views** let users inspect and edit arguments before the tool runs.
- **`ChatViewModel.RespondToApproval()`** supports passing modified arguments back to
  the tool invocation.

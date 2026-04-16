# Microsoft.Extensions.AI.Chat

Headless chat session engine for `Microsoft.Extensions.AI`. Manages conversation history, streaming responses, tool call tracking, and human-in-the-loop approval — no UI framework dependency.

## How It Works

### 1. Register the chat session

```csharp
builder.Services.AddChatSession(ServiceLifetime.Transient);
```

This registers `ChatSession` and `IChatSession`. Requires `IChatClient` and `IEnumerable<AITool>` to be registered in DI.

### 2. Use the session

```csharp
public class MyPage
{
    public ChatSession ChatSession { get; }

    public MyPage(ChatSession chatSession)
    {
        ChatSession = chatSession;
        ChatSession.SystemPrompt = "You are a helpful assistant.";
    }
}
```

```csharp
// Send a message — streams the response and fires Changed events
await chatSession.SendAsync("What plants are in my garden?");

// Clear conversation (resets history and pending approvals)
chatSession.Clear();
```

### 3. Listen for changes

```csharp
chatSession.Changed += (sender, e) =>
{
    switch (e.Kind)
    {
        case ChatSessionChangeKind.MessageAdded:
            // New message at e.Index
            break;
        case ChatSessionChangeKind.MessageUpdated:
            // Streaming text update at e.Index
            break;
        case ChatSessionChangeKind.Reset:
            // Session cleared
            break;
        case ChatSessionChangeKind.StateChanged:
            // IsBusy or approval state changed
            break;
    }
};
```

### 4. Handle approvals

When a tool has `ApprovalRequired = true`, the session emits a `ToolApprovalRequestContent` and pauses. The UI shows the request, and the user approves or rejects:

```csharp
// User approves
var response = request.CreateResponse(approved: true);
await chatSession.SubmitApprovalAsync(response);

// User rejects
var response = request.CreateResponse(approved: false, "Not now");
await chatSession.SubmitApprovalAsync(response);
```

Pending approvals are automatically rejected when the user sends a new message.

## Key Types

| Type | Description |
|---|---|
| `ChatSession` | Conversation engine — sends messages, streams responses, manages approvals |
| `IChatSession` | Interface for the session (used by UI controls) |
| `ChatEntry` | A single message in the transcript (content + role + approval state) |
| `ContentRole` | User, Assistant, Tool, Approval, Error |
| `ToolApprovalState` | None, Pending, Approved, Rejected |
| `ChatSessionChangedEventArgs` | Change notification with kind, entry, and index |

## Threading

`ChatSession.SendAsync` runs the streaming loop on a thread pool thread (required for Android's `NetworkOnMainThreadException`). UI consumers must marshal `Changed` events to their own thread via `Dispatcher.Dispatch` or equivalent.

## Manual Construction

You can create a `ChatSession` without DI for custom tool sets:

```csharp
var tools = CatalogTools.Default.GetTools(serviceProvider);
var chatClient = serviceProvider.GetRequiredService<IChatClient>();
var session = new ChatSession(tools, chatClient);
```

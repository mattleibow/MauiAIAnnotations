# Microsoft.Extensions.AI Attributes / Chat / Maui

Turn regular .NET services into AI-callable tools and host them in a reusable chat experience without hand-writing JSON schemas or tool adapters.

`Microsoft.Extensions.AI.Attributes` handles **reflection-based tool discovery** and DI registration.  
`Microsoft.Extensions.AI.Chat` adds the **headless `ChatSession` engine** and approval-aware conversation flow.  
`Microsoft.Extensions.AI.Maui` adds the **thin MAUI chat panel, content templates, and approval UI** on top.

| Windows | Android |
| --- | --- |
| <img src="images/home-page.png" alt="Garden Helper App on Windows" width="300" /> | <img src="images/home-page-android.png" alt="Garden Helper App on Android" width="300" /> |

## Start here

Choose the path that matches what you need:

| Path | Best for | Guide |
| --- | --- | --- |
| **Quick start** | Add AI chat + function calling to an existing MAUI page fast | [Getting Started](getting-started.md) |
| **Approval flow** | Require approve/reject before sensitive tools run | [Human-in-the-Loop Approval](human-in-the-loop.md) |
| **Tool result views** | Start with the default result view, then replace it with cards or widgets when needed | [Custom Tool Rendering](tool-rendering.md) |

## Packages

| Package | Description |
| --- | --- |
| **Microsoft.Extensions.AI.Attributes** | Attribute-based AI tool discovery and `AddAITools()` registration. Decorate methods with `[ExportAIFunction]` and expose them as AI tools. |
| **Microsoft.Extensions.AI.Chat** | Headless `ChatSession`, transcript types, and approval-aware chat orchestration that can be hosted anywhere. |
| **Microsoft.Extensions.AI.Maui** | Reusable MAUI chat UI, content template system, and human-in-the-loop approval dialogs that render an `IChatSession`. |

## Requirements

- .NET 10.0+
- `Microsoft.Extensions.AI` 10.4.1+
- `Microsoft.Extensions.DependencyInjection` 10.0.0+

# MauiAIAnnotations — Copilot Instructions

## Project Overview
A .NET 10 library for attribute-based AI tool discovery (`MauiAIAnnotations`) with a reusable MAUI chat UI library (`MauiAIAnnotations.Maui`) and a gardening helper sample app (`MauiSampleApp`).

## Build & Test

```bash
# Build everything (TreatWarningsAsErrors is on)
dotnet build MauiAIAnnotations.slnx

# Run unit tests (54 tests across 2 projects) — MUST PASS before pushing
dotnet test MauiAIAnnotations.slnx

# Build Windows target for running the sample app
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0

# Run sample app
dotnet run --project samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0 --no-build

# UI testing with MauiDevFlow (after app is running)
# ALL scenarios in tests/UI-TEST-PLAN.md MUST be run and confirmed passing before pushing
maui-devflow MAUI tree
maui-devflow MAUI screenshot
maui-devflow MAUI tap <AutomationId>
maui-devflow MAUI fill <AutomationId> "text"
```

### Pre-Push Checklist
1. `dotnet build MauiAIAnnotations.slnx` — 0 warnings, 0 errors
2. `dotnet test MauiAIAnnotations.slnx` — all tests pass
3. Launch app and run ALL scenarios in `tests/UI-TEST-PLAN.md` (1–10c) via MauiDevFlow
4. Every scenario must produce the expected result before code is pushed

## Architecture

### Project Structure
- `src/MauiAIAnnotations/` — Core library: `[ExportAIFunction]` attribute, `AddAITools()` DI extension, `DependencyInjectionAIFunction`
- `src/MauiAIAnnotations.Maui/` — Reusable MAUI chat UI: `ChatPanelControl`, `ChatPanelControl`, `ChatViewModel`, content template system
- `samples/MauiSampleApp/` — Gardening helper demo app
- `samples/MauiSampleApp.Core/` — Core services (PlantDataService, SpeciesService, SeasonsService)
- `tests/` — xUnit test projects

### Key Patterns

**Attribute-based tool discovery**: Decorate service methods with `[ExportAIFunction]`. `AddAITools()` scans assemblies and registers each as an `AITool` singleton in DI. `DependencyInjectionAIFunction` resolves the service from DI per-invocation, respecting lifetimes.

**Content template system**: `ContentTemplate` subclasses declare a `When(ContentContext)` predicate and a `ViewType`. `ContentTemplateSelector` picks the first match. Templates are declared in XAML and cached (MAUI requirement: same DataTemplate instance per call).

**Compiled bindings**: All chat content views use `x:DataType="chat:ContentContext"` for compiled bindings. Views bind directly to `ContentContext` computed properties (Text, FunctionName, ErrorMessage, ResultText, DisplayText). No intermediate ViewModels for library views.

**CommunityToolkit.Mvvm**: All ViewModels use `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`. No manual INotifyPropertyChanged.

**Permanent chat sidebar layout

### DI Registration (MauiProgram.cs should stay minimal)
```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools(); // discovers [ExportAIFunction] methods automatically
```

### AutomationIds (for MauiDevFlow)
- HomePage: `PageTitle`, `AddPlantButton`, `PlantList`
- AddPlantPage: `NicknameEntry`, `SpeciesEntry`, `LocationEntry`, `IndoorSwitch`, `SavePlantButton`
- PlantDetailPage: `CareHistoryList`, `DeletePlantButton`
- Chat: `ChatSidebar`, `ClearChatButton`, `ChatMessages`, `ChatInput`, `SendMessageButton`, `ChatBusyIndicator`
- Approval (generic): `ApproveToolButton`, `RejectToolButton`
- Approval (plant-specific): `ApprovalNicknameEntry`, `ApprovalSpeciesEntry`, `ApprovalLocationEntry`, `ApprovalIndoorSwitch`, `ApproveToolButton`, `RejectToolButton`
- Approval (batch care): `BatchCareList`, `ApproveToolButton`, `RejectToolButton`

## Conventions
- `TreatWarningsAsErrors` is enabled in `Directory.Build.props`
- Use `artifacts/` output directory (`UseArtifactsOutput`)
- XAML views in the library bind directly to `ContentContext` — no ViewModels unless doing real data transformation
- Chat auto-scroll uses `Dispatcher.DispatchDelayed(50ms)` before `ScrollTo`
- CollectionView items can't be tapped via MauiDevFlow (virtualization bounds are -1x-1)

## Documentation
- `docs/README.md` — Project overview, features, quick example
- `docs/getting-started.md` — Adding AI to an existing MAUI app (attributes → DI → chat sidebar)
- `docs/tool-rendering.md` — Custom content views for tool calls (PlantCardView example)
- `docs/human-in-the-loop.md` — Approval flow with `[ExportAIFunction(ApprovalRequired = true)]`
- `docs/images/` — Screenshots referenced by the docs
- When adding new features, update the relevant doc page and capture new screenshots via MauiDevFlow

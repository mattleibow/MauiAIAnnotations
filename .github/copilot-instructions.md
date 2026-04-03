# MauiAIAnnotations — Copilot Instructions

## Project Overview
A .NET 10 library for attribute-based AI tool discovery (`MauiAIAnnotations`) with a reusable MAUI chat UI library (`MauiAIAnnotations.Maui`) and a gardening helper sample app (`MauiSampleApp`).

## Build & Test

```bash
# Build everything (TreatWarningsAsErrors is on)
dotnet build MauiAIAnnotations.slnx

# Run unit tests (50 tests across 2 projects)
dotnet test MauiAIAnnotations.slnx

# Build Windows target for running the sample app
dotnet build samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0

# Run sample app
dotnet run --project samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0 --no-build

# UI testing with MauiDevFlow (after app is running)
# See tests/UI-TEST-PLAN.md for full test scenarios
maui-devflow MAUI tree
maui-devflow MAUI screenshot
maui-devflow MAUI tap <AutomationId>
maui-devflow MAUI fill <AutomationId> "text"
```

## Architecture

### Project Structure
- `src/MauiAIAnnotations/` — Core library: `[ExportAIFunction]` attribute, `AddAITools()` DI extension, `DependencyInjectionAIFunction`
- `src/MauiAIAnnotations.Maui/` — Reusable MAUI chat UI: `ChatOverlayControl`, `ChatPanelControl`, `ChatViewModel`, content template system
- `samples/MauiSampleApp/` — Gardening helper demo app
- `samples/MauiSampleApp.Core/` — Core services (PlantDataService, SpeciesService, SeasonsService)
- `tests/` — xUnit test projects

### Key Patterns

**Attribute-based tool discovery**: Decorate service methods with `[ExportAIFunction]`. `AddAITools()` scans assemblies and registers each as an `AITool` singleton in DI. `DependencyInjectionAIFunction` resolves the service from DI per-invocation, respecting lifetimes.

**Content template system**: `ContentTemplateMapping` subclasses declare a `When(ContentContext)` predicate and a `ViewType`. `ContentTemplateSelector` picks the first match. Templates are declared in XAML and cached (MAUI requirement: same DataTemplate instance per call).

**Compiled bindings**: All chat content views use `x:DataType="chat:ContentContext"` for compiled bindings. Views bind directly to `ContentContext` computed properties (Text, FunctionName, ErrorMessage, ResultText, DisplayText). No intermediate ViewModels for library views.

**CommunityToolkit.Mvvm**: All ViewModels use `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`. No manual INotifyPropertyChanged.

**Responsive chat**: `ChatOverlayControl` shows as sidebar (≥900px) or overlay (narrow). Both share the same `ChatVM` and content templates.

### DI Registration (MauiProgram.cs should stay minimal)
```csharp
builder.Services.AddSingleton<PlantDataService>();
builder.Services.AddAITools(); // discovers [ExportAIFunction] methods automatically
```

### AutomationIds (for MauiDevFlow)
- HomePage: `PageTitle`, `AddPlantButton`, `PlantList`
- AddPlantPage: `NicknameEntry`, `SpeciesEntry`, `LocationEntry`, `IndoorSwitch`, `SavePlantButton`
- PlantDetailPage: `CareHistoryList`, `DeletePlantButton`
- Chat: `ChatFabButton`, `ChatOverlayPanel`, `CloseChatButton`, `ClearChatButton`, `ChatMessages`, `ChatInput`, `SendMessageButton`, `ChatBusyIndicator`
- Sidebar: `SidebarClearChatButton`, `SidebarCloseChatButton`

## Conventions
- `TreatWarningsAsErrors` is enabled in `Directory.Build.props`
- Use `artifacts/` output directory (`UseArtifactsOutput`)
- XAML views in the library bind directly to `ContentContext` — no ViewModels unless doing real data transformation
- Chat auto-scroll uses `Dispatcher.DispatchDelayed(50ms)` before `ScrollTo`
- CollectionView items can't be tapped via MauiDevFlow (virtualization bounds are -1x-1)

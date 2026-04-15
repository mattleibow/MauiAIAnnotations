---
name: maui-ai-debugging
description: >
  End-to-end workflow for building, deploying, inspecting, and debugging .NET MAUI and MAUI Blazor Hybrid apps
  as an AI agent. Use when: (1) Building or running a MAUI app on iOS simulator, Android emulator, Mac Catalyst,
  macOS (AppKit), or Linux/GTK, (2) Inspecting or interacting with a running app's UI (visual tree, tapping,
  filling text, screenshots, property queries), (3) Debugging Blazor WebView content via CDP, (4) Managing
  simulators or emulators, (5) Setting up MAUI DevFlow in a MAUI project, (6) Completing a build-deploy-inspect-fix
  feedback loop, (7) Handling permission dialogs and system alerts, (8) Managing multiple simultaneous apps via
  the broker daemon. Covers: maui CLI (maui devflow), adb, xcrun simctl, xdotool,
  and dotnet build/run for all MAUI target platforms including macOS (AppKit) and Linux/GTK.
---

# MAUI AI Debugging

Build, deploy, inspect, and debug .NET MAUI apps from the terminal. This skill enables a complete
feedback loop: **build → deploy → inspect → fix → rebuild**.

## Prerequisites

```bash
dotnet tool install -g Microsoft.Maui.Cli --prerelease || dotnet tool update -g Microsoft.Maui.Cli --prerelease
```

## Integrating MAUI DevFlow into a MAUI App

1. Add NuGet package:
   ```xml
   <PackageReference Include="Microsoft.Maui.DevFlow.Agent" />
   ```
   - For **Blazor Hybrid**, also add `Microsoft.Maui.DevFlow.Blazor`
   - For **Linux/GTK apps**, use `Microsoft.Maui.DevFlow.Agent.Gtk` and `Microsoft.Maui.DevFlow.Blazor.Gtk` instead

2. Register in `MauiProgram.cs` inside `#if DEBUG`:
   ```csharp
   using Microsoft.Maui.DevFlow.Agent;

   #if DEBUG
   builder.AddMauiDevFlowAgent();
   #endif
   ```

3. For Mac Catalyst: ensure `network.server` entitlement
4. For Android: run `adb reverse` for broker + agent ports

## Core Workflow

### 1. Ensure a Device/Simulator/Emulator is Running

**iOS Simulator:**
```bash
maui apple simulator list
maui apple simulator start "iPhone 16 Pro"
```

**Android Emulator:**
```bash
maui android emulator start --name MyEmulator
```

### 2. Build and Deploy

```bash
# Mac Catalyst
dotnet build MyApp.csproj -f net10.0-maccatalyst -t:Run

# iOS Simulator
dotnet build MyApp.csproj -f net10.0-ios -t:Run -p:_DeviceName=:v2:udid=SIMULATOR_UDID

# Android
dotnet build MyApp.csproj -f net10.0-android -t:Install
adb shell am start -n com.companyname.myapp/crc64HASH.MainActivity
```

### 3. Inspect and Interact

```bash
# Dump the visual tree
maui devflow MAUI tree

# Take a screenshot
maui devflow MAUI screenshot

# Tap an element by AutomationId
maui devflow MAUI tap MyButton

# Fill text
maui devflow MAUI fill MyEntry "Hello"

# Start MCP server for AI agent integration
maui devflow mcp
```

### 4. Android-specific Setup

```bash
# Forward ports for DevFlow agent communication
adb forward tcp:9223 tcp:9223
```

## CLI Commands Reference

| Command | Description |
|---------|-------------|
| `maui doctor` | Run environment diagnostics |
| `maui device list` | List connected devices and emulators |
| `maui devflow MAUI tree` | Dump the visual tree of a running MAUI app |
| `maui devflow MAUI screenshot` | Take a screenshot of a running MAUI app |
| `maui devflow MAUI tap` | Tap an element |
| `maui devflow MAUI fill` | Fill text into an element |
| `maui devflow cdp` | Blazor WebView automation via Chrome DevTools Protocol |
| `maui devflow mcp` | Start MCP server for AI agent integration |
| `maui devflow broker` | Manage the DevFlow agent broker |
| `maui android install` | Full interactive Android environment setup |
| `maui android emulator create` | Create an Android emulator |
| `maui android emulator start` | Start an Android emulator |
| `maui apple simulator list` | List simulator devices (macOS only) |
| `maui apple simulator start` | Boot a simulator (macOS only) |

## NuGet Packages

| Package | Description |
|---------|-------------|
| `Microsoft.Maui.DevFlow.Agent` | In-app agent for .NET MAUI apps |
| `Microsoft.Maui.DevFlow.Agent.Core` | Platform-agnostic core |
| `Microsoft.Maui.DevFlow.Agent.Gtk` | GTK/Linux agent |
| `Microsoft.Maui.DevFlow.Blazor` | Blazor WebView CDP bridge |
| `Microsoft.Maui.DevFlow.Blazor.Gtk` | Blazor CDP bridge for WebKitGTK |
| `Microsoft.Maui.DevFlow.Logging` | Buffered rotating JSONL file logger |

## Platform Support

| Platform | Status |
|----------|--------|
| Mac Catalyst | ✅ |
| iOS Simulator | ✅ |
| Linux/GTK | ✅ |
| Android | 🔄 In progress |
| Windows | 🔄 In progress |

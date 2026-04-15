# UI Test Plan — MauiDevFlow Scenarios

Run these scenarios after every significant change. They serve the same purpose as `dotnet test` but for the running app UI.

## Prerequisites
- App built and running: `dotnet run --project samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0`
- MauiDevFlow CLI available: `maui-devflow MAUI tree` responds

---

## Scenario 1: App Launches Correctly

**Steps:**
```bash
maui-devflow MAUI tree
```

**Expected:**
- HomePage is visible with bounds > 0
- `PageTitle` label shows "My Garden"
- `AddPlantButton` is visible
- `PlantList` CollectionView is visible
- The collapsed `HomeChatTray` is visible at the bottom
- `ChatTrayToggleButton` is visible

**Pass criteria:** Home page elements and the collapsed chat tray are present in tree output.

---

## Scenario 2: Chat Tray Opens

**Steps:**
```bash
maui-devflow MAUI tap ChatTrayToggleButton
maui-devflow MAUI tree
```

**Expected:**
- `HomeChatTray` expands to most of the page height
- `ChatTrayScrim` becomes visible behind it
- `ClearChatButton` is visible in the tray header
- `ChatMessages` CollectionView visible
- `ChatInput` Entry visible
- `SendMessageButton` visible

**Pass criteria:** Expanded tray and chat panel elements are visible in tree.

---

## Scenario 3: Send a Message

**Steps:**
```bash
maui-devflow MAUI fill ChatInput "Hello, can you help me?"
maui-devflow MAUI tap SendMessageButton
# Wait 10 seconds for AI response
maui-devflow MAUI screenshot
```

**Expected:**
- User message bubble (green, right-aligned) shows "Hello, can you help me?"
- Assistant response bubble (light, left-aligned) appears
- Auto-scroll positioned at bottom

**Pass criteria:** Screenshot shows both user and assistant messages.

---

## Scenario 4: Function Calls Render

**Steps:**
```bash
maui-devflow MAUI fill ChatInput "What time is it and what gardening advice for April in northern hemisphere?"
maui-devflow MAUI tap SendMessageButton
# Wait 15 seconds for AI + tool calls
maui-devflow MAUI screenshot
```

**Expected:**
- User message bubble
- ⚙️ "Calling get_current_datetime..." function call bubble
- Function result bubble with current date/time
- ⚙️ "Calling get_seasonal_gardening_advice..." function call bubble
- Function result bubble with season advice
- Assistant text with gardening tips

**Pass criteria:** At least 2 function call bubbles + 2 result bubbles visible.

---

## Scenario 5: Plant Card Template Renders

**Steps:**
```bash
maui-devflow MAUI fill ChatInput "Tell me about Baby Tomatoes"
maui-devflow MAUI tap SendMessageButton
# Wait 15 seconds
maui-devflow MAUI screenshot
maui-devflow MAUI tree
```

**Expected:**
- AI calls `get_plant` function
- **PlantResultView** (custom plant card) renders in chat — NOT a generic text FunctionResultView
- Plant card shows nickname ("Baby Tomatoes"), location, indoor status
- Tree contains a `PlantCardView` element as a descendant of `ChatMessages`

**Pass criteria:** PlantCardView visible in tree under ChatMessages. Screenshot shows styled plant card, not plain text result.

---

## Scenario 5b: Multi-Plant Preview Renders

**Steps:**
```bash
maui-devflow MAUI tap ChatTrayToggleButton
maui-devflow MAUI fill ChatInput "Show me all my tomato plants"
maui-devflow MAUI tap SendMessageButton
# Wait 15 seconds
maui-devflow MAUI tree
```

**Expected:**
- AI uses `get_plants` with a tomato-oriented query
- `PlantResultView` renders a **horizontal CollectionView** rather than a single card
- Tree contains `PlantPreviewList` under `ChatMessages`
- Multiple `PlantCardView` items appear in the horizontal strip

**Pass criteria:** `PlantPreviewList` and multiple plant cards are visible in the tree.

---

## Scenario 5c: Missing Plant Shows Friendly Empty State

**Steps:**
```bash
maui-devflow MAUI tap ChatTrayToggleButton
maui-devflow MAUI fill ChatInput "Show me all my tomato plants"
maui-devflow MAUI tap SendMessageButton
# Wait 15 seconds
maui-devflow MAUI tree
```

**Expected:**
- AI uses `get_plants` with a tomato-oriented query
- If there are no matching plants, the chat renders `PlantEmptyState`
- The UI shows a friendly message such as "No plants matched that request."
- The chat does **not** fall back to a raw `[]` function-result bubble

**Pass criteria:** Missing plant queries render the plant empty state instead of raw JSON/text output.

---

## Scenario 6: Clear Chat

**Steps:**
```bash
maui-devflow MAUI tap ClearChatButton
maui-devflow MAUI screenshot
```

**Expected:**
- All messages cleared from ChatMessages
- Chat input still visible and functional

**Pass criteria:** Screenshot shows empty chat area.

---

## Scenario 7: Collapse and Reopen Chat Tray

**Steps:**
```bash
# Send a message first
maui-devflow MAUI fill ChatInput "Hi"
maui-devflow MAUI tap SendMessageButton
# Wait 5s
maui-devflow MAUI tap ChatTrayToggleButton
# Verify the tray is collapsed
maui-devflow MAUI tree
maui-devflow MAUI tap ChatTrayToggleButton
maui-devflow MAUI screenshot
```

**Expected:**
- After collapsing: Home page stays visible
- After reopening: Previous messages still present (session persistence)

**Pass criteria:** Messages persist across tray collapse/reopen.

---

## Scenario 8: Plant Detail Navigation

**Note:** CollectionView items cannot be tapped via MauiDevFlow (virtualization bounds are -1x-1). This scenario requires manual interaction or programmatic navigation.

**Manual steps:**
1. Click on a plant card in the list
2. Verify PlantDetailPage loads with species info, water/sun/frost badges
3. Verify Quick Actions tab has care buttons
4. Verify Care History tab shows events
5. Tap "‹ Back" to return to HomePage

**Pass criteria:** PlantDetailPage shows correct plant data.

---

## Scenario 9: Add Plant Flow

**Steps:**
```bash
maui-devflow MAUI tap AddPlantButton
# Wait for navigation
maui-devflow MAUI tree
maui-devflow MAUI fill NicknameEntry "Test Rose"
maui-devflow MAUI fill SpeciesEntry "rose"
maui-devflow MAUI fill LocationEntry "Front garden"
maui-devflow MAUI tap SavePlantButton
# Wait for save + navigation back
maui-devflow MAUI tree
```

**Expected:**
- AddPlantPage loads with NicknameEntry, SpeciesEntry, LocationEntry, IndoorSwitch, SavePlantButton
- After save: navigates back to HomePage
- PlantList now contains "Test Rose"

**Pass criteria:** New plant appears in PlantList on HomePage.

---

## Scenario 10a: Custom Approval — Add Plant (Review and Approve)

**Prerequisites:** `add_plant` is `ApprovalRequired = true` and has a custom `ToolApprovalTemplate`

**Steps:**
```bash
maui-devflow MAUI tap ChatTrayToggleButton
maui-devflow MAUI fill ChatInput "Add a new plant called Sun Daisy, species daisy, balcony, outdoor"
maui-devflow MAUI tap SendMessageButton
# Wait 18 seconds for AI to propose the tool call
maui-devflow MAUI screenshot
maui-devflow MAUI tree
```

**Expected:**
- User message appears
- **Custom approval card** (PlantApprovalView) with editable details:
  - Nickname: "Sun Daisy" — `ApprovalNicknameEntry`
  - Species: "daisy" — `ApprovalSpeciesEntry`
  - Location: "balcony" — `ApprovalLocationEntry`
  - Indoor value — `ApprovalIndoorSwitch`
  - ✅ Approve / ❌ Reject buttons (`ApproveToolButton`, `RejectToolButton`)
- Tree contains `PlantApprovalView` (NOT `ToolApprovalView`) under ChatMessages

**Approve:**
```bash
maui-devflow MAUI tap ApproveToolButton
# Wait 15 seconds for function execution + AI response
maui-devflow MAUI screenshot
maui-devflow MAUI tree
```

**Expected after approve:**
- Approval card stays in the chat with a resolved state such as "✅ Approved — add_plant"
- A ⚙️ "Calling add_plant..." function call bubble appears after approval
- PlantResultView card shows the plant using the current approved values
- Assistant confirms the addition
- Tree still contains the resolved `PlantApprovalView`, but only the active pending approval uses the plain button IDs

**Pass criteria:** Custom approval card renders, the values can be reviewed or adjusted before approval, and the approved plant is added.

---

## Scenario 10b: Generic Approval — Remove Plant (Approve)

**Prerequisites:** `remove_plant` is `ApprovalRequired = true` and has NO custom template (uses generic `ToolApprovalView`)

**Steps:**
```bash
maui-devflow MAUI tap ClearChatButton
maui-devflow MAUI fill ChatInput "Remove the plant called Golden Daisy"
maui-devflow MAUI tap SendMessageButton
# Wait 18 seconds
maui-devflow MAUI screenshot
maui-devflow MAUI tree
```

**Expected:**
- **Generic approval card** (ToolApprovalView) with:
  - "⚠️ Approval Required" header
  - "Tool: remove_plant"
  - ✅ Approve / ❌ Reject buttons
  - NO editable fields
- Tree contains `ToolApprovalView` (NOT `PlantApprovalView`) under ChatMessages

**Approve:**
```bash
maui-devflow MAUI tap ApproveToolButton
# Wait 15 seconds
maui-devflow MAUI screenshot
```

**Expected after approve:**
- Approval card stays in the chat with a resolved state such as "✅ Approved — remove_plant"
- A ⚙️ "Calling remove_plant..." function call bubble appears after approval
- Assistant confirms the removal
- Close chat, verify plant is removed from PlantList

**Pass criteria:** Generic approval rendered, card replaced after approve, plant removed.

---

## Scenario 10c: Generic Approval — Remove Plant (Reject)

**Steps:**
```bash
maui-devflow MAUI tap ClearChatButton
maui-devflow MAUI fill ChatInput "Remove the plant called Sunny Basil"
maui-devflow MAUI tap SendMessageButton
# Wait 18 seconds
maui-devflow MAUI tap RejectToolButton
# Wait 5 seconds
maui-devflow MAUI screenshot
```

**Expected after reject:**
- Approval card stays in the chat with a resolved state such as "❌ Rejected — remove_plant"
- Assistant explains that the removal was not carried out
- Close chat, verify Sunny Basil is **still** in PlantList

**Pass criteria:** Rejection state is shown, and the plant is NOT removed.

---

## Running the Full Plan

```bash
# 1. Build and run tests
dotnet build MauiAIAnnotations.slnx
dotnet test MauiAIAnnotations.slnx

# 2. Launch app
dotnet run --project samples/MauiSampleApp/MauiSampleApp.csproj -f net10.0-windows10.0.19041.0 --no-build &

# 3. Wait for app to start, then run scenarios 1-10
# Each scenario should be run in order as they may depend on app state from previous scenarios
```

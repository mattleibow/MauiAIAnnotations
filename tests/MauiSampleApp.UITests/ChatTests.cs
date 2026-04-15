using Microsoft.Maui.DevFlow.Driver;
using MauiSampleApp.UITests.Infrastructure;

namespace MauiSampleApp.UITests;

/// <summary>
/// Scenarios 2, 3, 4, 5, 5b, 5c, 6, 7 from UI-TEST-PLAN.md — Chat tray and messaging.
/// </summary>
[Collection("DevFlow")]
public class ChatTests
{
    private readonly DevFlowFixture _fixture;

    public ChatTests(DevFlowFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Scenario 2: Chat Tray Opens
    /// Verifies chat panel elements are visible (permanent sidebar or via toggle).
    /// </summary>
    [Fact]
    public async Task ChatTray_OpensOnToggle()
    {
        var driver = _fixture.Driver;

        // Ensure chat tray is open (handles both toggle and permanent sidebar)
        await driver.EnsureChatTrayOpenAsync();

        // Verify chat panel elements are present
        Assert.True(await driver.IsElementVisibleAsync("ClearChatButton"),
            "ClearChatButton should be visible in expanded tray");
        Assert.True(await driver.IsElementVisibleAsync("ChatMessages"),
            "ChatMessages CollectionView should be visible");
        Assert.True(await driver.IsElementVisibleAsync("ChatInput"),
            "ChatInput Entry should be visible");
        Assert.True(await driver.IsElementVisibleAsync("SendMessageButton"),
            "SendMessageButton should be visible");
    }

    /// <summary>
    /// Scenario 3: Send a Message
    /// Sends a message and verifies both user and assistant messages appear.
    /// Requires a running AI backend.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task Chat_SendMessage_ShowsResponse()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();

        // Clear any existing chat
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Send a message
        await driver.SendChatMessageAsync("Hello, can you help me?");

        // Wait for AI response (generous timeout for LLM)
        await Task.Delay(15_000);

        // Get tree and look for both user and assistant messages
        var tree = await driver.GetTreeAsync(25);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // The tree should have at least 2 items (user + assistant)
        var messageCount = CountVisibleChildren(chatMessages);
        Assert.True(messageCount >= 2,
            $"Expected at least 2 messages (user + assistant), found {messageCount}");
    }

    /// <summary>
    /// Scenario 4: Function Calls Render
    /// Sends a message that triggers function calls and verifies function call/result bubbles.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task Chat_FunctionCallsRender()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Send a message that should trigger tool calls
        await driver.SendChatMessageAsync(
            "What time is it and what gardening advice for April in northern hemisphere?");

        // Wait for AI + tool calls
        await Task.Delay(20_000);

        // Check tree for function call content
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // Look for FunctionCallView or similar elements in the tree
        var hasFunctionContent = HasElementOfType(chatMessages, "FunctionCallView")
            || HasElementOfType(chatMessages, "FunctionResultView")
            || HasElementOfType(chatMessages, "ToolCallView");

        Assert.True(hasFunctionContent,
            "Expected function call/result bubbles in the chat after tool-invoking message");
    }

    /// <summary>
    /// Scenario 5: Plant Card Template Renders
    /// Asks about a specific plant and verifies a PlantCardView renders instead of generic text.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task Chat_PlantCardTemplateRenders()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        await driver.SendChatMessageAsync("Tell me about Baby Tomatoes");
        await Task.Delay(20_000);

        // Look for PlantCardView in the tree
        var plantCard = await driver.FindElementByTypeAsync("PlantCardView", maxDepth: 30);
        Assert.NotNull(plantCard);
    }

    /// <summary>
    /// Scenario 5b: Multi-Plant Preview Renders
    /// Asks about multiple plants and verifies a horizontal CollectionView of plant cards appears.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task Chat_MultiPlantPreviewRenders()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        await driver.SendChatMessageAsync("Show me all my tomato plants");
        await Task.Delay(20_000);

        // Look for PlantPreviewList (horizontal CollectionView) in the tree
        var tree = await driver.GetTreeAsync(30);
        var previewList = FindElementByAutomationId(tree, "PlantPreviewList");

        if (previewList is not null)
        {
            Assert.True(previewList.IsVisible, "PlantPreviewList should be visible");
            // Should contain multiple PlantCardView items
            Assert.True(HasElementOfType(previewList, "PlantCardView"),
                "PlantPreviewList should contain PlantCardView items");
        }
        else
        {
            // Fallback: at least one PlantCardView should be present
            var card = await driver.FindElementByTypeAsync("PlantCardView");
            Assert.NotNull(card);
        }
    }

    /// <summary>
    /// Scenario 5c: Missing Plant Shows Friendly Empty State
    /// Asks about plants that don't exist and verifies a friendly empty state renders.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task Chat_MissingPlantShowsEmptyState()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        await driver.SendChatMessageAsync("Show me all my orchid plants");
        await Task.Delay(20_000);

        // Look for PlantEmptyState in the tree
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        var hasEmptyState = HasElementOfType(chatMessages, "PlantEmptyState");
        var hasPlantResult = HasElementOfType(chatMessages, "PlantResultView");

        // Either the empty state shows, or the PlantResultView handles the empty case
        Assert.True(hasEmptyState || hasPlantResult,
            "Expected PlantEmptyState or PlantResultView for a missing plant query, " +
            "not raw JSON/text output");
    }

    /// <summary>
    /// Scenario 6: Clear Chat
    /// Taps ClearChatButton and verifies all messages are removed.
    /// </summary>
    [Fact]
    public async Task Chat_ClearButton_RemovesMessages()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();

        // Clear chat
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Verify ChatInput is still visible (chat is functional)
        Assert.True(await driver.IsElementVisibleAsync("ChatInput"),
            "ChatInput should still be visible after clearing");

        // Verify ChatMessages area is empty (no child content)
        var tree = await driver.GetTreeAsync(25);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        var visibleChildren = CountVisibleChildren(chatMessages);
        Assert.Equal(0, visibleChildren);
    }

    /// <summary>
    /// Scenario 7: Collapse and Reopen Chat Tray
    /// Sends a message, collapses the tray, reopens it, and verifies messages persist.
    /// </summary>
    [Trait("Category", "AI")]
    [Fact]
    public async Task ChatTray_PersistsMessagesAcrossCollapseReopen()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Send a message
        await driver.SendChatMessageAsync("Hi");
        await Task.Delay(8_000);

        // Count messages before collapse
        var treeBefore = await driver.GetTreeAsync(25);
        var chatBefore = FindElementByAutomationId(treeBefore, "ChatMessages");
        var countBefore = chatBefore is not null ? CountVisibleChildren(chatBefore) : 0;
        Assert.True(countBefore >= 2, $"Expected at least 2 messages before collapse, got {countBefore}");

        // Try collapse — permanent sidebar layouts may not support this
        await driver.EnsureChatTrayClosedAsync();
        await Task.Delay(500);

        var chatInputAfterCollapse = await driver.QueryAsync(automationId: "ChatInput");
        if (chatInputAfterCollapse is null or { Count: 0 })
        {
            // Tray is collapsible — verify full collapse/reopen cycle
            Assert.True(await driver.IsElementVisibleAsync("PageTitle"),
                "PageTitle should still be visible with collapsed tray");

            // Reopen
            await driver.EnsureChatTrayOpenAsync();
        }
        // else: permanent sidebar, chat stays visible — skip collapse assertions

        // Verify messages persisted
        var treeAfter = await driver.GetTreeAsync(25);
        var chatAfter = FindElementByAutomationId(treeAfter, "ChatMessages");
        var countAfter = chatAfter is not null ? CountVisibleChildren(chatAfter) : 0;
        Assert.Equal(countBefore, countAfter);
    }

    #region Helpers

    private static ElementInfo? FindElementByAutomationId(IList<ElementInfo>? elements, string automationId)
    {
        if (elements is null) return null;
        foreach (var el in elements)
        {
            if (el.AutomationId == automationId) return el;
            var child = FindElementByAutomationId(el.Children, automationId);
            if (child is not null) return child;
        }
        return null;
    }

    private static bool HasElementOfType(ElementInfo root, string typeName)
    {
        if (root.Type?.Contains(typeName, StringComparison.OrdinalIgnoreCase) == true)
            return true;
        if (root.Children is null) return false;
        return root.Children.Any(c => HasElementOfType(c, typeName));
    }

    private static int CountVisibleChildren(ElementInfo parent)
    {
        if (parent.Children is null) return 0;
        return parent.Children.Count(c => c.IsVisible);
    }

    #endregion
}

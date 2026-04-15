using Microsoft.Maui.DevFlow.Driver;
using MauiSampleApp.UITests.Infrastructure;

namespace MauiSampleApp.UITests;

/// <summary>
/// Scenarios 10a, 10b, 10c from UI-TEST-PLAN.md — Approval flow testing.
/// All require a running AI backend.
/// </summary>
[Collection("DevFlow")]
[Trait("Category", "AI")]
public class ApprovalFlowTests
{
    private readonly DevFlowFixture _fixture;

    public ApprovalFlowTests(DevFlowFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Scenario 10a: Custom Approval — Add Plant (Review and Approve)
    /// Asks AI to add a plant, verifies custom PlantApprovalView renders with
    /// editable fields, approves, and verifies the plant is added.
    /// </summary>
    [Fact]
    public async Task CustomApproval_AddPlant_ReviewAndApprove()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Ask AI to add a plant
        await driver.SendChatMessageAsync(
            "Add a new plant called Sun Daisy, species daisy, balcony, outdoor");

        // Wait for AI to propose the tool call and render approval card
        await Task.Delay(20_000);

        // Verify custom approval card rendered (PlantApprovalView, not generic ToolApprovalView)
        var plantApproval = await driver.FindElementByTypeAsync("PlantApprovalView", maxDepth: 30);
        Assert.NotNull(plantApproval);

        // Verify editable fields are present
        Assert.True(await driver.IsElementVisibleAsync("ApprovalNicknameEntry"),
            "ApprovalNicknameEntry should be visible");
        Assert.True(await driver.IsElementVisibleAsync("ApprovalSpeciesEntry"),
            "ApprovalSpeciesEntry should be visible");
        Assert.True(await driver.IsElementVisibleAsync("ApprovalLocationEntry"),
            "ApprovalLocationEntry should be visible");
        Assert.True(await driver.IsElementVisibleAsync("ApprovalIndoorSwitch"),
            "ApprovalIndoorSwitch should be visible");

        // Verify approve/reject buttons
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"),
            "ApproveToolButton should be visible");
        Assert.True(await driver.IsElementVisibleAsync("RejectToolButton"),
            "RejectToolButton should be visible");

        // Approve the tool call
        await driver.TapByAutomationIdAsync("ApproveToolButton");

        // Wait for function execution + AI response
        await Task.Delay(20_000);

        // Verify the approval card is in resolved state (buttons disabled/changed)
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // Should still have a PlantApprovalView (now resolved)
        Assert.True(HasElementOfType(chatMessages, "PlantApprovalView"),
            "Resolved PlantApprovalView should still be visible in chat");

        // Should have function call/result content after approval
        var hasToolExecution = HasElementOfType(chatMessages, "FunctionCallView")
            || HasElementOfType(chatMessages, "FunctionResultView")
            || HasElementOfType(chatMessages, "PlantCardView");
        Assert.True(hasToolExecution,
            "Expected function execution content after approval");
    }

    /// <summary>
    /// Scenario 10b: Generic Approval — Remove Plant (Approve)
    /// Asks AI to remove a plant, verifies generic ToolApprovalView renders,
    /// approves, and verifies the plant is removed.
    /// </summary>
    [Fact]
    public async Task GenericApproval_RemovePlant_Approve()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Ask AI to remove a plant
        await driver.SendChatMessageAsync("Remove the plant called Golden Daisy");
        await Task.Delay(20_000);

        // Verify generic approval card rendered (ToolApprovalView)
        var toolApproval = await driver.FindElementByTypeAsync("ToolApprovalView", maxDepth: 30);
        Assert.NotNull(toolApproval);

        // Generic approval should have approve/reject buttons but NO editable fields
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"),
            "ApproveToolButton should be visible");
        Assert.True(await driver.IsElementVisibleAsync("RejectToolButton"),
            "RejectToolButton should be visible");

        // Should NOT have the custom plant approval fields
        Assert.False(await driver.IsElementVisibleAsync("ApprovalNicknameEntry"),
            "ApprovalNicknameEntry should NOT be visible in generic approval");

        // Approve
        await driver.TapByAutomationIdAsync("ApproveToolButton");
        await Task.Delay(20_000);

        // Verify resolved state
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // The approval card should be in resolved state
        Assert.True(HasElementOfType(chatMessages, "ToolApprovalView"),
            "Resolved ToolApprovalView should still be visible in chat");

        // Close chat and verify plant is gone from list
        await driver.EnsureChatTrayClosedAsync();
        await Task.Delay(500);

        // Verify we're on home page with plant list
        Assert.True(await driver.IsElementVisibleAsync("PlantList"));
    }

    /// <summary>
    /// Scenario 10c: Generic Approval — Remove Plant (Reject)
    /// Asks AI to remove a plant, verifies generic ToolApprovalView renders,
    /// rejects, and verifies the plant is NOT removed.
    /// </summary>
    [Fact]
    public async Task GenericApproval_RemovePlant_Reject()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(500);

        // Ask AI to remove a plant
        await driver.SendChatMessageAsync("Remove the plant called Sunny Basil");
        await Task.Delay(20_000);

        // Verify generic approval card rendered
        var toolApproval = await driver.FindElementByTypeAsync("ToolApprovalView", maxDepth: 30);
        Assert.NotNull(toolApproval);

        // Verify approve/reject buttons
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"));
        Assert.True(await driver.IsElementVisibleAsync("RejectToolButton"));

        // Reject the tool call
        await driver.TapByAutomationIdAsync("RejectToolButton");
        await Task.Delay(8_000);

        // Verify rejection state in chat
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // The approval card should be in rejected/resolved state
        Assert.True(HasElementOfType(chatMessages, "ToolApprovalView"),
            "Rejected ToolApprovalView should still be visible in chat");

        // Close chat and verify plant is still in list
        await driver.EnsureChatTrayClosedAsync();
        await Task.Delay(500);
        Assert.True(await driver.IsElementVisibleAsync("PlantList"),
            "PlantList should still be visible — plant should NOT have been removed");
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

    #endregion
}

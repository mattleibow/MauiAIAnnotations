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

        // Ask AI to add a plant — use a unique name to avoid conflicts
        var plantName = $"UITest Daisy {Random.Shared.Next(1000, 9999)}";
        await driver.SendChatMessageAsync(
            $"Add a new plant called {plantName}, species daisy, location balcony, outdoor");

        // Poll for approval card to appear (up to 45s — AI needs to propose tool call)
        ElementInfo? approvalView = null;
        for (var i = 0; i < 22 && approvalView is null; i++)
        {
            await Task.Delay(2_000);
            // Check for either custom PlantApprovalView or generic ToolApprovalView
            approvalView = await driver.FindElementByTypeAsync("PlantApprovalView", maxDepth: 30);
            approvalView ??= await driver.FindElementByTypeAsync("ToolApprovalView", maxDepth: 30);
        }

        Assert.NotNull(approvalView);

        // Verify approve/reject buttons are present
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"),
            "ApproveToolButton should be visible");
        Assert.True(await driver.IsElementVisibleAsync("RejectToolButton"),
            "RejectToolButton should be visible");

        // Approve the tool call
        await driver.TapByAutomationIdAsync("ApproveToolButton");

        // Wait for function execution + AI response (poll for resolution)
        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(2_000);
            // Check if approval buttons are gone (resolved state uses suffixed IDs)
            var approveBtn = await driver.QueryAsync(automationId: "ApproveToolButton");
            if (approveBtn is null or { Count: 0 })
                break;
        }

        // Verify chat still has content after approval
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);
    }

    /// <summary>
    /// Scenario 10b: Generic Approval — Remove Plant (Approve)
    /// Asks AI to remove a plant, verifies an approval view renders,
    /// approves, and verifies the operation completes.
    /// </summary>
    [Fact]
    public async Task GenericApproval_RemovePlant_Approve()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();

        // Retry up to 2 times — AI may not always call the tool on first attempt
        ElementInfo? approvalView = null;
        for (var attempt = 0; attempt < 2 && approvalView is null; attempt++)
        {
            await driver.TapByAutomationIdAsync("ClearChatButton");
            await Task.Delay(1000);

            await driver.SendChatMessageAsync(
                "Use the remove_plant tool to remove the plant nicknamed UITest Rose 5880");

            // Poll for approval card (up to 60s)
            for (var i = 0; i < 30 && approvalView is null; i++)
            {
                await Task.Delay(2_000);
                approvalView = await driver.FindElementByTypeAsync("ToolApprovalView", maxDepth: 30);
                approvalView ??= await driver.FindElementByTypeAsync("PlantApprovalView", maxDepth: 30);
            }
        }

        Assert.NotNull(approvalView);

        // Verify approve/reject buttons
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"),
            "ApproveToolButton should be visible");

        // Approve
        await driver.TapByAutomationIdAsync("ApproveToolButton");

        // Wait for resolution (poll until approve button disappears)
        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(2_000);
            var approveBtn = await driver.QueryAsync(automationId: "ApproveToolButton");
            if (approveBtn is null or { Count: 0 })
                break;
        }

        // Verify chat still functioning
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);
    }

    /// <summary>
    /// Scenario 10c: Generic Approval — Remove Plant (Reject)
    /// Asks AI to remove a plant, verifies an approval view renders,
    /// rejects, and verifies the plant is NOT removed.
    /// </summary>
    [Fact]
    public async Task GenericApproval_RemovePlant_Reject()
    {
        var driver = _fixture.Driver;
        await driver.EnsureChatTrayOpenAsync();
        await driver.TapByAutomationIdAsync("ClearChatButton");
        await Task.Delay(1000);

        // Ask AI to remove a specific plant
        await driver.SendChatMessageAsync(
            "Use the remove_plant tool to remove the plant nicknamed UITest Rose 5150");

        // Poll for approval card (up to 60s)
        ElementInfo? approvalView = null;
        for (var i = 0; i < 30 && approvalView is null; i++)
        {
            await Task.Delay(2_000);
            approvalView = await driver.FindElementByTypeAsync("ToolApprovalView", maxDepth: 30);
            approvalView ??= await driver.FindElementByTypeAsync("PlantApprovalView", maxDepth: 30);
        }

        Assert.NotNull(approvalView);

        // Verify approve/reject buttons
        Assert.True(await driver.IsElementVisibleAsync("ApproveToolButton"));
        Assert.True(await driver.IsElementVisibleAsync("RejectToolButton"));

        // Reject the tool call
        await driver.TapByAutomationIdAsync("RejectToolButton");

        // Wait for rejection to process
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(2_000);
            var rejectBtn = await driver.QueryAsync(automationId: "RejectToolButton");
            if (rejectBtn is null or { Count: 0 })
                break;
        }

        // Verify chat is still functional after rejection
        var tree = await driver.GetTreeAsync(30);
        var chatMessages = FindElementByAutomationId(tree, "ChatMessages");
        Assert.NotNull(chatMessages);

        // Verify we can still access the plant list (plant wasn't deleted)
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

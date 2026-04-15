using Microsoft.Maui.DevFlow.Driver;

namespace MauiSampleApp.UITests.Infrastructure;

/// <summary>
/// Helper extension methods for common DevFlow UI testing patterns.
/// </summary>
public static class DevFlowExtensions
{
    /// <summary>
    /// Waits for an element with the given AutomationId to appear in the visual tree.
    /// Polls every <paramref name="intervalMs"/> ms up to <paramref name="timeoutMs"/> ms.
    /// </summary>
    public static async Task<ElementInfo> WaitForElementAsync(
        this IAppDriver driver,
        string automationId,
        int timeoutMs = 10_000,
        int intervalMs = 500)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var results = await driver.QueryAsync(automationId: automationId);
            if (results is { Count: > 0 })
                return results[0];

            await Task.Delay(intervalMs);
        }

        throw new TimeoutException(
            $"Element with AutomationId '{automationId}' not found within {timeoutMs}ms.");
    }

    /// <summary>
    /// Waits for an element with the given AutomationId to disappear from the visual tree.
    /// </summary>
    public static async Task WaitForElementGoneAsync(
        this IAppDriver driver,
        string automationId,
        int timeoutMs = 10_000,
        int intervalMs = 500)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var results = await driver.QueryAsync(automationId: automationId);
            if (results is null or { Count: 0 })
                return;

            await Task.Delay(intervalMs);
        }

        throw new TimeoutException(
            $"Element with AutomationId '{automationId}' still present after {timeoutMs}ms.");
    }

    /// <summary>
    /// Taps an element by AutomationId, waiting for it to appear first.
    /// </summary>
    public static async Task TapByAutomationIdAsync(
        this IAppDriver driver,
        string automationId,
        int timeoutMs = 10_000)
    {
        var element = await driver.WaitForElementAsync(automationId, timeoutMs);
        await driver.TapAsync(element.Id);
    }

    /// <summary>
    /// Fills text into an element by AutomationId, waiting for it to appear first.
    /// </summary>
    public static async Task FillByAutomationIdAsync(
        this IAppDriver driver,
        string automationId,
        string text,
        int timeoutMs = 10_000)
    {
        var element = await driver.WaitForElementAsync(automationId, timeoutMs);
        await driver.FillAsync(element.Id, text);
    }

    /// <summary>
    /// Gets the text property of an element by AutomationId.
    /// </summary>
    public static async Task<string?> GetTextByAutomationIdAsync(
        this IAppDriver driver,
        string automationId,
        int timeoutMs = 10_000)
    {
        var element = await driver.WaitForElementAsync(automationId, timeoutMs);
        return element.Text;
    }

    /// <summary>
    /// Queries for an element and returns true if it exists and is visible.
    /// </summary>
    public static async Task<bool> IsElementVisibleAsync(
        this IAppDriver driver,
        string automationId)
    {
        var results = await driver.QueryAsync(automationId: automationId);
        return results is { Count: > 0 } && results[0].IsVisible;
    }

    /// <summary>
    /// Searches the visual tree recursively for an element of the given type name.
    /// </summary>
    public static async Task<ElementInfo?> FindElementByTypeAsync(
        this IAppDriver driver,
        string typeName,
        int maxDepth = 30)
    {
        var tree = await driver.GetTreeAsync(maxDepth);
        return FindByType(tree, typeName);
    }

    /// <summary>
    /// Waits for an element of the given type to appear in the tree.
    /// </summary>
    public static async Task<ElementInfo> WaitForElementByTypeAsync(
        this IAppDriver driver,
        string typeName,
        int timeoutMs = 20_000,
        int intervalMs = 1000,
        int maxDepth = 30)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var tree = await driver.GetTreeAsync(maxDepth);
            var found = FindByType(tree, typeName);
            if (found is not null)
                return found;

            await Task.Delay(intervalMs);
        }

        throw new TimeoutException(
            $"Element of type '{typeName}' not found within {timeoutMs}ms.");
    }

    /// <summary>
    /// Sends a chat message by filling the ChatInput and tapping SendMessageButton.
    /// </summary>
    public static async Task SendChatMessageAsync(
        this IAppDriver driver,
        string message,
        int waitAfterSendMs = 0)
    {
        await driver.FillByAutomationIdAsync("ChatInput", message);
        await driver.TapByAutomationIdAsync("SendMessageButton");
        if (waitAfterSendMs > 0)
            await Task.Delay(waitAfterSendMs);
    }

    /// <summary>
    /// Opens the chat tray if it's not already open.
    /// </summary>
    public static async Task EnsureChatTrayOpenAsync(this IAppDriver driver)
    {
        var chatInput = await driver.QueryAsync(automationId: "ChatInput");
        if (chatInput is null or { Count: 0 })
        {
            await driver.TapByAutomationIdAsync("ChatTrayToggleButton");
            await driver.WaitForElementAsync("ChatInput", timeoutMs: 5000);
        }
    }

    /// <summary>
    /// Closes the chat tray if it's currently open.
    /// </summary>
    public static async Task EnsureChatTrayClosedAsync(this IAppDriver driver)
    {
        var chatInput = await driver.QueryAsync(automationId: "ChatInput");
        if (chatInput is { Count: > 0 })
        {
            await driver.TapByAutomationIdAsync("ChatTrayToggleButton");
            await driver.WaitForElementGoneAsync("ChatInput", timeoutMs: 5000);
        }
    }

    private static ElementInfo? FindByType(IList<ElementInfo>? elements, string typeName)
    {
        if (elements is null) return null;

        foreach (var el in elements)
        {
            if (el.Type?.Contains(typeName, StringComparison.OrdinalIgnoreCase) == true)
                return el;

            var child = FindByType(el.Children, typeName);
            if (child is not null)
                return child;
        }

        return null;
    }
}

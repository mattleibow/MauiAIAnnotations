namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Interface for custom approval content views that provide editable arguments.
/// Implement this on a ContentView to integrate with <see cref="ToolApprovalView"/>.
/// The wrapper provides the header, tool name, and Approve/Reject buttons.
/// Your view provides only the editable content area.
/// </summary>
public interface IApprovalContentProvider
{
    /// <summary>
    /// Initialize the view with the approval request arguments.
    /// </summary>
    void Initialize(IDictionary<string, object?>? arguments);

    /// <summary>
    /// Returns the (potentially modified) arguments to submit on approval.
    /// </summary>
    IDictionary<string, object?> GetArguments();

    /// <summary>
    /// Called when the approval is resolved. Disable editing controls.
    /// </summary>
    void SetReadOnly(bool readOnly);
}

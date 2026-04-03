namespace MauiAIAnnotations.Maui.Chat;

/// <summary>
/// Implemented by a view or its BindingContext to receive the <see cref="ContentContext"/>
/// when the approval card is shown. Works like MAUI's <c>IQueryAttributable</c> —
/// the library checks both the view and its BindingContext.
/// </summary>
public interface IContentContextAware
{
    /// <summary>
    /// Called by the library when the content is applied.
    /// Use this to extract tool arguments and populate ViewModel properties.
    /// </summary>
    void ApplyContentContext(ContentContext context);
}

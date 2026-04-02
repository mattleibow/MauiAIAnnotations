using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace MauiAIAnnotations;

/// <summary>
/// Provides a collection of AI tools that can be used with an <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// Implementations discover and create <see cref="AITool"/> instances from
/// service methods annotated with <see cref="ExportAIFunctionAttribute"/>.
/// Consumers set <c>ChatOptions.Tools</c> to the result of <see cref="GetTools"/>.
/// </remarks>
public interface IAIToolProvider
{
    /// <summary>
    /// Gets the collection of AI tools available for use.
    /// </summary>
    /// <returns>A read-only list of AI tools.</returns>
    IReadOnlyList<AITool> GetTools();
}

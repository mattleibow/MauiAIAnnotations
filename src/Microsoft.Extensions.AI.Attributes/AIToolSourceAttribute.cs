namespace Microsoft.Extensions.AI.Attributes;

/// <summary>
/// Marks a service type whose <see cref="ExportAIFunctionAttribute"/>-decorated methods should
/// be included in an <see cref="AIToolContext"/>. Apply this attribute to a partial class that
/// inherits from <see cref="AIToolContext"/> — the source generator will scan the specified type
/// and emit tool creation code.
/// </summary>
/// <remarks>
/// Multiple <see cref="AIToolSourceAttribute"/> instances can be applied to a single context
/// to aggregate tools from several service types.
/// <code>
/// [AIToolSource(typeof(PlantDataService))]
/// [AIToolSource(typeof(SeasonsService))]
/// public partial class GardenTools : AIToolContext { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AIToolSourceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance targeting the specified service type.
    /// </summary>
    /// <param name="sourceType">
    /// The type whose <see cref="ExportAIFunctionAttribute"/>-decorated methods
    /// should be discovered by the source generator.
    /// </param>
    public AIToolSourceAttribute(Type sourceType)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
    }

    /// <summary>
    /// Gets the service type whose methods are scanned for <see cref="ExportAIFunctionAttribute"/>.
    /// </summary>
    public Type SourceType { get; }
}

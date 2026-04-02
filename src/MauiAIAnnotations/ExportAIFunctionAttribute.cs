using System;

namespace MauiAIAnnotations;

/// <summary>
/// Marks a method to be exported as an AI tool function.
/// Methods with this attribute are discovered by <see cref="IAIToolProvider"/>
/// and made available to AI chat clients as callable tools.
/// </summary>
/// <remarks>
/// The method must be a public instance method on a type that is registered in DI.
/// Parameter descriptions should use <see cref="System.ComponentModel.DescriptionAttribute"/>.
/// Return values are automatically serialized by Microsoft.Extensions.AI.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ExportAIFunctionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance with default name (derived from method name).
    /// </summary>
    public ExportAIFunctionAttribute() { }

    /// <summary>
    /// Initializes a new instance with an explicit tool name.
    /// </summary>
    /// <param name="name">The name of the tool as exposed to the AI model.</param>
    public ExportAIFunctionAttribute(string name) => Name = name;

    /// <summary>
    /// The tool name exposed to the AI model (e.g. "get_plants").
    /// If not set, defaults to the method name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description shown to the AI model explaining what this tool does.
    /// If not set, falls back to <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// on the method, if present.
    /// </summary>
    public string? Description { get; set; }
}

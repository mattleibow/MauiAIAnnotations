using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.AI.Attributes.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class AIToolContextGenerator : IIncrementalGenerator
{
    private const string AIToolContextFullName = "Microsoft.Extensions.AI.Attributes.AIToolContext";
    private const string AIToolSourceAttributeFullName = "Microsoft.Extensions.AI.Attributes.AIToolSourceAttribute";
    private const string ExportAIFunctionAttributeFullName = "Microsoft.Extensions.AI.Attributes.ExportAIFunctionAttribute";
    private const string DescriptionAttributeFullName = "System.ComponentModel.DescriptionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var contextDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AIToolSourceAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax cds &&
                    cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, ct) => GetContextModel(ctx, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!);

        // Group by context class (multiple [AIToolSource] attributes produce multiple entries)
        var grouped = contextDeclarations
            .Collect()
            .SelectMany(static (items, _) =>
            {
                var dict = new Dictionary<string, ContextModel>();
                foreach (var item in items)
                {
                    var key = item.FullyQualifiedName;
                    if (!dict.TryGetValue(key, out var existing))
                    {
                        dict[key] = item;
                    }
                    else
                    {
                        var merged = new ContextModel(
                            existing.Namespace,
                            existing.ClassName,
                            existing.FullyQualifiedName,
                            existing.Accessibility,
                            existing.SourceTypes.AddRange(
                                item.SourceTypes.Where(s =>
                                    !existing.SourceTypes.Any(e =>
                                        e.FullyQualifiedName == s.FullyQualifiedName))));
                        dict[key] = merged;
                    }
                }
                return dict.Values.ToImmutableArray();
            });

        context.RegisterSourceOutput(grouped, static (spc, model) =>
        {
            var source = GenerateContextSource(model);
            spc.AddSource($"{model.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static ContextModel? GetContextModel(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol contextSymbol)
            return null;

        if (!InheritsFrom(contextSymbol, AIToolContextFullName))
            return null;

        var sourceTypes = new List<SourceTypeModel>();

        foreach (var attr in contextSymbol.GetAttributes())
        {
            ct.ThrowIfCancellationRequested();

            if (attr.AttributeClass?.ToDisplayString() != AIToolSourceAttributeFullName)
                continue;

            if (attr.ConstructorArguments.Length != 1)
                continue;

            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol sourceTypeSymbol)
                continue;

            var methods = GetExportedMethods(sourceTypeSymbol, ct);
            if (methods.Count > 0)
            {
                sourceTypes.Add(new SourceTypeModel(
                    sourceTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    methods.ToImmutableArray()));
            }
        }

        if (sourceTypes.Count == 0)
            return null;

        var accessibility = contextSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Private => "private",
            _ => "internal"
        };

        return new ContextModel(
            contextSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            contextSymbol.Name,
            contextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            accessibility,
            sourceTypes.ToImmutableArray());
    }

    private static List<MethodModel> GetExportedMethods(INamedTypeSymbol typeSymbol, CancellationToken ct)
    {
        var methods = new List<MethodModel>();

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            if (member is not IMethodSymbol method)
                continue;

            if (method.MethodKind != MethodKind.Ordinary)
                continue;

            if (method.IsStatic)
                continue;

            ExportAIFunctionData? exportData = null;

            foreach (var attr in method.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == ExportAIFunctionAttributeFullName)
                {
                    var name = (string?)null;
                    var description = (string?)null;
                    var approvalRequired = false;

                    if (attr.ConstructorArguments.Length >= 1 &&
                        attr.ConstructorArguments[0].Value is string ctorName)
                    {
                        name = ctorName;
                    }

                    foreach (var namedArg in attr.NamedArguments)
                    {
                        switch (namedArg.Key)
                        {
                            case "Name":
                                name = namedArg.Value.Value as string;
                                break;
                            case "Description":
                                description = namedArg.Value.Value as string;
                                break;
                            case "ApprovalRequired":
                                approvalRequired = namedArg.Value.Value is true;
                                break;
                        }
                    }

                    exportData = new ExportAIFunctionData(
                        name ?? method.Name,
                        description,
                        approvalRequired);
                    break;
                }
            }

            if (exportData is null)
                continue;

            // Check for [Description] on the method as fallback
            if (exportData.Description is null)
            {
                foreach (var attr in method.GetAttributes())
                {
                    if (attr.AttributeClass?.ToDisplayString() == DescriptionAttributeFullName &&
                        attr.ConstructorArguments.Length >= 1 &&
                        attr.ConstructorArguments[0].Value is string desc)
                    {
                        exportData = new ExportAIFunctionData(
                            exportData.Name,
                            desc,
                            exportData.ApprovalRequired);
                        break;
                    }
                }
            }

            methods.Add(new MethodModel(
                method.Name,
                exportData.Name,
                exportData.Description,
                exportData.ApprovalRequired));
        }

        return methods;
    }

    private static bool InheritsFrom(INamedTypeSymbol symbol, string baseTypeFullName)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString() == baseTypeFullName)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static string GenerateContextSource(ContextModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var indent = "";
        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
            indent = "    ";
        }

        sb.AppendLine($"{indent}{model.Accessibility} partial class {model.ClassName}");
        sb.AppendLine($"{indent}{{");

        // Static Default property
        sb.AppendLine($"{indent}    /// <summary>Gets the default singleton instance of this tool context.</summary>");
        sb.AppendLine($"{indent}    public static {model.ClassName} Default {{ get; }} = new {model.ClassName}();");
        sb.AppendLine();

        // GetTools override — uses base class CreateDITool helper
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public override global::System.Collections.Generic.IReadOnlyList<global::Microsoft.Extensions.AI.AITool> GetTools(global::System.IServiceProvider serviceProvider)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return new global::Microsoft.Extensions.AI.AITool[]");
        sb.AppendLine($"{indent}        {{");

        foreach (var sourceType in model.SourceTypes)
        {
            foreach (var method in sourceType.Methods)
            {
                sb.AppendLine($"{indent}            CreateDITool(serviceProvider, typeof({sourceType.FullyQualifiedName}), {Escape(method.MethodName)}, {Escape(method.ToolName)}, {EscapeOrNull(method.Description)}, {BoolLiteral(method.ApprovalRequired)}),");
            }
        }

        sb.AppendLine($"{indent}        }};");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // RegisterTools (non-keyed) — uses base class RegisterDITool helper
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public override void RegisterTools(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine($"{indent}    {{");

        foreach (var sourceType in model.SourceTypes)
        {
            foreach (var method in sourceType.Methods)
            {
                sb.AppendLine($"{indent}        RegisterDITool(services, typeof({sourceType.FullyQualifiedName}), {Escape(method.MethodName)}, {Escape(method.ToolName)}, {EscapeOrNull(method.Description)}, {BoolLiteral(method.ApprovalRequired)});");
            }
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // RegisterTools (keyed) — uses base class RegisterKeyedDITool helper
        sb.AppendLine($"{indent}    /// <inheritdoc />");
        sb.AppendLine($"{indent}    public override void RegisterTools(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, string key)");
        sb.AppendLine($"{indent}    {{");

        foreach (var sourceType in model.SourceTypes)
        {
            foreach (var method in sourceType.Methods)
            {
                sb.AppendLine($"{indent}        RegisterKeyedDITool(services, key, typeof({sourceType.FullyQualifiedName}), {Escape(method.MethodName)}, {Escape(method.ToolName)}, {EscapeOrNull(method.Description)}, {BoolLiteral(method.ApprovalRequired)});");
            }
        }

        sb.AppendLine($"{indent}    }}");

        sb.AppendLine($"{indent}}}");

        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string EscapeOrNull(string? value)
    {
        return value is null ? "null" : Escape(value);
    }

    private static string BoolLiteral(bool value)
    {
        return value ? "true" : "false";
    }

    // Pipeline data models

    private sealed record ExportAIFunctionData(
        string Name,
        string? Description,
        bool ApprovalRequired);

    private sealed record MethodModel(
        string MethodName,
        string ToolName,
        string? Description,
        bool ApprovalRequired);

    private sealed record SourceTypeModel(
        string FullyQualifiedName,
        ImmutableArray<MethodModel> Methods);

    private sealed record ContextModel(
        string Namespace,
        string ClassName,
        string FullyQualifiedName,
        string Accessibility,
        ImmutableArray<SourceTypeModel> SourceTypes);
}

using System.ComponentModel;
using Microsoft.Extensions.AI.Attributes;

namespace Microsoft.Extensions.AI.Attributes.Tests;

internal sealed class TestToolService
{
    [Description("A test tool")]
    [ExportAIFunction("test_tool")]
    public string DoSomething([Description("input value")] string input) => $"result: {input}";

    [ExportAIFunction]
    public int GetCount() => 42;

    [Description("An async tool")]
    [ExportAIFunction("async_tool")]
    public async Task<string> DoAsyncWork([Description("input value")] string input)
    {
        await Task.Delay(1);
        return $"async: {input}";
    }

    public void InternalMethod() { }
}

internal sealed class MultiParamService
{
    [Description("A tool with multiple parameters")]
    [ExportAIFunction("multi_param")]
    public string Combine(
        [Description("first name")] string firstName,
        [Description("last name")] string lastName,
        [Description("age in years")] int age)
        => $"{firstName} {lastName}, age {age}";
}

internal sealed class DisposableToolService : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;

    [Description("Tool on a disposable service")]
    [ExportAIFunction("disposable_tool")]
    public string GetValue() => "value";
}

[Description("Service-level description")]
internal sealed class DescriptionFallbackService
{
    [ExportAIFunction("fallback_desc")]
    [Description("Method-level description from DescriptionAttribute")]
    public string Work() => "done";
}

internal sealed class NoAttributeService
{
    public string DoWork() => "no attribute";
}

internal abstract class AbstractService
{
    [ExportAIFunction("abstract_tool")]
    public string DoWork() => "abstract";
}

internal sealed class InvocationCounterService
{
    public int InvocationCount { get; private set; }

    [Description("Counts invocations")]
    [ExportAIFunction("counter_tool")]
    public int Increment()
    {
        InvocationCount++;
        return InvocationCount;
    }
}

internal sealed class CancellableToolService
{
    [Description("A cancellable tool")]
    [ExportAIFunction("cancellable_tool")]
    public async Task<string> CancellableWork(
        [Description("input value")] string input,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(1, cancellationToken);
        return $"done: {input}";
    }
}

internal sealed class GenericMethodService
{
    [ExportAIFunction("bad_generic")]
    public T GenericMethod<T>() => default!;
}

internal sealed class RefParameterService
{
    [ExportAIFunction("bad_ref")]
    public void RefMethod(ref string x) { }
}

internal sealed class ApprovalMixedService
{
    [Description("A safe read-only tool")]
    [ExportAIFunction("safe_read")]
    public string ReadData() => "data";

    [Description("A dangerous write tool")]
    [ExportAIFunction("dangerous_write", ApprovalRequired = true)]
    public string WriteData([Description("data to write")] string data) => $"wrote: {data}";

    [Description("Another safe tool")]
    [ExportAIFunction("another_safe")]
    public int GetCount() => 1;
}

internal sealed class AllApprovalService
{
    [Description("Needs approval")]
    [ExportAIFunction("needs_approval", ApprovalRequired = true)]
    public string DoWork() => "done";
}

internal sealed class ComplexPlantRequest
{
    [Description("friendly nickname shown to the user")]
    public string Nickname { get; set; } = string.Empty;

    [Description("botanical species or variety")]
    public string Species { get; set; } = string.Empty;

    [Description("current location of the plant")]
    public string Location { get; set; } = string.Empty;

    [Description("whether the plant lives indoors")]
    public bool IsIndoor { get; set; }
}

internal sealed class PlantToolResult
{
    [Description("stable identifier returned to the AI")]
    public string Id { get; set; } = string.Empty;

    [Description("nickname echoed back to the AI")]
    public string Nickname { get; set; } = string.Empty;
}

internal sealed class ComplexSchemaService
{
    [Description("Creates a plant profile from structured details.")]
    [ExportAIFunction("create_plant_profile", ApprovalRequired = true)]
    public PlantToolResult CreatePlantProfile(
        [Description("structured details for the plant profile")] ComplexPlantRequest profile,
        [Description("whether to notify the user after creation")] bool notifyUser = true) =>
        new()
        {
            Id = "plant-123",
            Nickname = profile.Nickname,
        };
}

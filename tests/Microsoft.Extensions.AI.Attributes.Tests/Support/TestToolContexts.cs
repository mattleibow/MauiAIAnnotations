using Microsoft.Extensions.AI.Attributes;

namespace Microsoft.Extensions.AI.Attributes.Tests;

[AIToolSource(typeof(TestToolService))]
internal partial class TestToolContext : AIToolContext { }

[AIToolSource(typeof(InvocationCounterService))]
internal partial class InvocationCounterToolContext : AIToolContext { }

[AIToolSource(typeof(MultiParamService))]
internal partial class MultiParamToolContext : AIToolContext { }

[AIToolSource(typeof(DisposableToolService))]
internal partial class DisposableToolContext : AIToolContext { }

[AIToolSource(typeof(DescriptionFallbackService))]
internal partial class DescriptionFallbackToolContext : AIToolContext { }

[AIToolSource(typeof(CancellableToolService))]
internal partial class CancellableToolContext : AIToolContext { }

[AIToolSource(typeof(ComplexSchemaService))]
internal partial class ComplexSchemaToolContext : AIToolContext { }

[AIToolSource(typeof(AllApprovalService))]
internal partial class AllApprovalToolContext : AIToolContext { }

[AIToolSource(typeof(ApprovalMixedService))]
internal partial class ApprovalMixedToolContext : AIToolContext { }

[AIToolSource(typeof(TestToolService))]
[AIToolSource(typeof(MultiParamService))]
internal partial class CompositeToolContext : AIToolContext { }

[AIToolSource(typeof(TestToolService))]
[AIToolSource(typeof(DisposableToolService))]
[AIToolSource(typeof(DescriptionFallbackService))]
internal partial class RegistrationTestToolContext : AIToolContext { }

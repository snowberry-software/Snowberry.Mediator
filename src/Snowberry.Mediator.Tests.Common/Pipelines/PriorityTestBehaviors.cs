using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

// Dynamic priority behavior for testing
public class DynamicPriorityBehavior : IPipelineBehavior<PriorityTestRequest, string>
{
    private readonly string _name;
    private readonly int _priority;

    public DynamicPriorityBehavior(string name, int priority)
    {
        _name = name;
        _priority = priority;
    }

    public async ValueTask<string> HandleAsync(PriorityTestRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(_name);
        return await NextPipeline(request, cancellationToken);
    }

    public PipelineHandlerDelegate<PriorityTestRequest, string> NextPipeline { get; set; } = null!;
}

// Same priority behaviors
[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorA : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorA));
        int result = await NextPipeline(request, cancellationToken);
        return result + 1;
    }

    public PipelineHandlerDelegate<MultiBehaviorRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorB : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorB));
        int result = await NextPipeline(request, cancellationToken);
        return result + 1;
    }

    public PipelineHandlerDelegate<MultiBehaviorRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorC : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorC));
        int result = await NextPipeline(request, cancellationToken);
        return result + 1;
    }

    public PipelineHandlerDelegate<MultiBehaviorRequest, int> NextPipeline { get; set; } = null!;
}

public class NoPriorityBehaviorA : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(NoPriorityBehaviorA));
        int result = await NextPipeline(request, cancellationToken);
        return result + 1;
    }

    public PipelineHandlerDelegate<MultiBehaviorRequest, int> NextPipeline { get; set; } = null!;
}

public class NoPriorityBehaviorB : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(NoPriorityBehaviorB));
        int result = await NextPipeline(request, cancellationToken);
        return result + 1;
    }

    public PipelineHandlerDelegate<MultiBehaviorRequest, int> NextPipeline { get; set; } = null!;
}
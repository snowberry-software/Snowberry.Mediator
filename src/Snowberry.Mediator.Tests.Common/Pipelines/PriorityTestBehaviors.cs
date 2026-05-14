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

    public async ValueTask<string> HandleAsync<TNext>(PriorityTestRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<PriorityTestRequest, string>
    {
        PipelineExecutionTracker.RecordExecution(_name);
        return await next.InvokeAsync(request, cancellationToken);
    }
}

// Same priority behaviors
[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorA : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(MultiBehaviorRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MultiBehaviorRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorA));
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1;
    }
}

[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorB : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(MultiBehaviorRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MultiBehaviorRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorB));
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1;
    }
}

[PipelineOverwritePriority(Priority = 100)]
public class SamePriorityBehaviorC : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(MultiBehaviorRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MultiBehaviorRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(SamePriorityBehaviorC));
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1;
    }
}

public class NoPriorityBehaviorA : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(MultiBehaviorRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MultiBehaviorRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(NoPriorityBehaviorA));
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1;
    }
}

public class NoPriorityBehaviorB : IPipelineBehavior<MultiBehaviorRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(MultiBehaviorRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MultiBehaviorRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(NoPriorityBehaviorB));
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1;
    }
}

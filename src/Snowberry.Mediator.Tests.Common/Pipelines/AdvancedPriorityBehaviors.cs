using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

// Performance testing behavior
public class PerformancePipelineBehavior : IPipelineBehavior<PerformanceTestRequest, int>
{
    private readonly string _name;
    private readonly int _priority;

    public PerformancePipelineBehavior(string name, int priority)
    {
        _name = name;
        _priority = priority;
    }

    public async ValueTask<int> HandleAsync<TNext>(PerformanceTestRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<PerformanceTestRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(_name);
        int result = await next.InvokeAsync(request, cancellationToken);
        return result + 1; // Each behavior adds 1
    }
}

// Inheritance testing behaviors
public class BasePipelineBehavior : IPipelineBehavior<InheritanceTestRequest, string>
{
    public virtual async ValueTask<string> HandleAsync<TNext>(InheritanceTestRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<InheritanceTestRequest, string>
    {
        PipelineExecutionTracker.RecordExecution(nameof(BasePipelineBehavior));
        string result = await next.InvokeAsync(request, cancellationToken);
        return $"Base({result})";
    }
}

[PipelineOverwritePriority(Priority = 200)]
public class DerivedPipelineBehavior : BasePipelineBehavior
{
    public override async ValueTask<string> HandleAsync<TNext>(InheritanceTestRequest request, TNext next, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(DerivedPipelineBehavior));
        string result = await next.InvokeAsync(request, cancellationToken);
        return $"Derived({result})";
    }
}

[PipelineOverwritePriority(Priority = 300)]
public class GrandChildPipelineBehavior : DerivedPipelineBehavior
{
    public override async ValueTask<string> HandleAsync<TNext>(InheritanceTestRequest request, TNext next, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(GrandChildPipelineBehavior));
        string result = await next.InvokeAsync(request, cancellationToken);
        return $"GrandChild({result})";
    }
}

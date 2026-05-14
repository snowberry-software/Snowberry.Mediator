using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = 50)]
public class MediumPriorityCounterRequestPipelineBehavior : IPipelineBehavior<CounterRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(CounterRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<CounterRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(MediumPriorityCounterRequestPipelineBehavior));
        int response = await next.InvokeAsync(request, cancellationToken);
        return response + 10;
    }
}

using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = int.MaxValue)]
public class AlwaysFirstCounterRequestPipelineBehavior : IPipelineBehavior<CounterRequest, int>
{
    /// <inheritdoc/>
    public async ValueTask<int> HandleAsync<TNext>(CounterRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<CounterRequest, int>
    {
        PipelineExecutionTracker.RecordExecution(nameof(AlwaysFirstCounterRequestPipelineBehavior));
        int response = await next.InvokeAsync(request, cancellationToken);
        return response + 1;
    }
}

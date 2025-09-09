using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = 10)]
public class LowPriorityCounterRequestPipelineBehavior : IPipelineBehavior<CounterRequest, int>
{
    public async ValueTask<int> HandleAsync(CounterRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(LowPriorityCounterRequestPipelineBehavior));
        int response = await NextPipeline(request, cancellationToken);
        return response + 100;
    }

    public PipelineHandlerDelegate<CounterRequest, int> NextPipeline { get; set; } = null!;
}
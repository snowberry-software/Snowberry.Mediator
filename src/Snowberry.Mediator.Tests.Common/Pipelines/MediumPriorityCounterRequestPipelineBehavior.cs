using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = 50)]
public class MediumPriorityCounterRequestPipelineBehavior : IPipelineBehavior<CounterRequest, int>
{
    public async ValueTask<int> HandleAsync(CounterRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(MediumPriorityCounterRequestPipelineBehavior));
        int response = await NextPipeline(request, cancellationToken);
        return response + 10;
    }

    public PipelineHandlerDelegate<CounterRequest, int> NextPipeline { get; set; } = null!;
}
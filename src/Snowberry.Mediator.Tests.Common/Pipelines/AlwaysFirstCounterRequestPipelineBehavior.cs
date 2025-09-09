using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = int.MaxValue)]
public class AlwaysFirstCounterRequestPipelineBehavior : IPipelineBehavior<CounterRequest, int>
{
    /// <inheritdoc/>
    public async ValueTask<int> HandleAsync(CounterRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(AlwaysFirstCounterRequestPipelineBehavior));
        int response = await NextPipeline(request, cancellationToken);
        return response + 1;
    }

    /// <inheritdoc/>
    public PipelineHandlerDelegate<CounterRequest, int> NextPipeline { get; set; } = null!;
}

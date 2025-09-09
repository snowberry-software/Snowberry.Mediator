using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = 100)]
public class HighPriorityStreamPipelineBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(HighPriorityStreamPipelineBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 2; // Double the values
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}
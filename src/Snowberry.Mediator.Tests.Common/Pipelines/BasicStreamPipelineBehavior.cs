using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class BasicStreamPipelineBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(BasicStreamPipelineBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 1000; // Add 1000 to each value
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}
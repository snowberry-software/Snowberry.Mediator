using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class SlowStreamPipelineBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(SlowStreamPipelineBehavior));

        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Add artificial delay to each item
            await Task.Delay(5, cancellationToken);
            yield return item + 100; // Transform the value
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}
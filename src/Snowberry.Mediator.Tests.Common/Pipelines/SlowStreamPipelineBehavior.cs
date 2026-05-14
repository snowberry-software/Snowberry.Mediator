using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class SlowStreamPipelineBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(SlowStreamPipelineBehavior));

        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Add artificial delay to each item
            await Task.Delay(5, cancellationToken);
            yield return item + 100;
        }
    }
}

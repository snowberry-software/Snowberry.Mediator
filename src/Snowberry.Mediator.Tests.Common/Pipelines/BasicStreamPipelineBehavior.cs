using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class BasicStreamPipelineBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(BasicStreamPipelineBehavior));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 1000;
        }
    }
}

using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class GenericStreamPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync<TNext>(TRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>
    {
        StreamPipelineExecutionTracker.RecordExecution($"GenericStreamPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
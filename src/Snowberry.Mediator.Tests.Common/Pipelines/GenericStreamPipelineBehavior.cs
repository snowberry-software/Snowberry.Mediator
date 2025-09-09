using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class GenericStreamPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution($"GenericStreamPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        await foreach (var item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public StreamPipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}
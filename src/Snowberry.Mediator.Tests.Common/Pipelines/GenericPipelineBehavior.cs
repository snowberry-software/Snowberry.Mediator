using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution($"GenericPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        var response = await NextPipeline(request, cancellationToken);
        return response;
    }

    public PipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}
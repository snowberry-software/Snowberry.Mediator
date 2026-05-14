using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
    {
        PipelineExecutionTracker.RecordExecution($"GenericPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        var response = await next.InvokeAsync(request, cancellationToken);
        return response;
    }
}
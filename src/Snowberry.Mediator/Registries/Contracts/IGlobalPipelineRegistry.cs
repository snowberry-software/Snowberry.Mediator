using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;

namespace Snowberry.Mediator.Registries.Contracts;

/// <summary>
/// The contract for the global pipeline registry.
/// </summary>
public interface IGlobalPipelineRegistry : IBaseGlobalPipelineRegistry<PipelineBehaviorHandlerInfo>
{
    /// <summary>
    /// Executes the pipeline behavior.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="handler">The request handler.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TRequest, TResponse>;
}

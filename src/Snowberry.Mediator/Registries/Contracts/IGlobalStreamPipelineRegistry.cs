using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;

namespace Snowberry.Mediator.Registries.Contracts;

/// <summary>
/// The contract for the global stream pipeline registry.
/// </summary>
public interface IGlobalStreamPipelineRegistry : IBaseGlobalPipelineRegistry<StreamPipelineBehaviorHandlerInfo>
{
    /// <summary>
    /// Executes the pipeline behavior.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="handler">The request handler.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    IAsyncEnumerable<TResponse> ExecuteAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IStreamRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IStreamRequest<TRequest, TResponse>;
}
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
    /// Executes the registered stream pipeline behaviors for the request and then invokes the terminal <paramref name="handler"/>.
    /// </summary>
    /// <typeparam name="TRequest">The stream request type.</typeparam>
    /// <typeparam name="TResponse">The response element type produced by the stream.</typeparam>
    /// <param name="serviceProvider">The service provider used to resolve each stream pipeline behavior.</param>
    /// <param name="handler">The terminal stream request handler invoked after all behaviors have run.</param>
    /// <param name="request">The request to dispatch through the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> that yields the response elements of type <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> ExecuteAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IStreamRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IStreamRequest<TRequest, TResponse>;
}
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
    /// Executes the registered pipeline behaviors for the request and then invokes the terminal <paramref name="handler"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type returned for the request.</typeparam>
    /// <param name="serviceProvider">The service provider used to resolve each pipeline behavior.</param>
    /// <param name="handler">The terminal request handler invoked after all behaviors have run.</param>
    /// <param name="request">The request to dispatch through the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that produces the response of type <typeparamref name="TResponse"/>.</returns>
    ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : class, IRequest<TRequest, TResponse>;
}
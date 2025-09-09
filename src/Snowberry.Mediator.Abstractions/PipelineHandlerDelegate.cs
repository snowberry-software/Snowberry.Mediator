using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions;

/// <summary>
/// Represents a delegate that defines a handler for processing requests and returning a response asynchronously.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="request">The request.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>A task that represents the asynchronous operation, containing the <typeparamref name="TResponse"/>.</returns>
public delegate ValueTask<TResponse> PipelineHandlerDelegate<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken = default)
    where TRequest : class, IRequest<TRequest, TResponse>;
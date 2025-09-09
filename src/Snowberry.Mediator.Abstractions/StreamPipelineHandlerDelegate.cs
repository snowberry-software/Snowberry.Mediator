using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions;

/// <summary>
/// Represents a delegate that defines a handler for processing streaming requests and returning an asynchronous stream of responses.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="request">The request.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
public delegate IAsyncEnumerable<TResponse> StreamPipelineHandlerDelegate<TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken = default)
    where TRequest : IStreamRequest<TRequest, TResponse>;
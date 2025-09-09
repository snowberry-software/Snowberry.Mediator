using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Mediator;

/// <summary>
/// Contract for sending requests and creating streams through the mediator.
/// </summary>
public interface IMediatorSender
{
    /// <summary>
    /// Sends a request through the mediator and returns a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the <typeparamref name="TResponse"/>.</returns>
    ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        IRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TRequest, TResponse>;

    /// <summary>
    /// Creates an asynchronous stream of responses of type <typeparamref name="TResponse"/> from a stream request.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous enumerable of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> CreateStreamAsync<TRequest, TResponse>(
        IStreamRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken = default)
        where TRequest : class, IStreamRequest<TRequest, TResponse>;
}

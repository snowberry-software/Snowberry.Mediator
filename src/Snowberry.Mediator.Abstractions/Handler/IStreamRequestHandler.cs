using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Handler;

/// <summary>
/// Contract for handling stream requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TRequest, TResponse>
{
    /// <summary>
    /// Handles the stream request and returns an asynchronous stream of responses.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

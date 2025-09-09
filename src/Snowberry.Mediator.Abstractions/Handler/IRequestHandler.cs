using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Handler;

/// <summary>
/// Contract for handling requests.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request and returns a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation.</param>
    /// <returns>A task representing the asynchronous operation, containing the <typeparamref name="TResponse"/>.</returns>
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for the next-step continuation passed to <see cref="IPipelineBehavior{TRequest,TResponse}.HandleAsync{TNext}"/>.
/// </summary>
/// <remarks>
/// Implementations are supplied by the mediator infrastructure; callers should only invoke <see cref="InvokeAsync"/>.
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineContinuation<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <summary>
    /// Invokes the next step in the pipeline. If this is the final continuation in the chain, the terminal
    /// <see cref="Handler.IRequestHandler{TRequest,TResponse}"/> is invoked.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the <typeparamref name="TResponse"/>.</returns>
    ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken);
}
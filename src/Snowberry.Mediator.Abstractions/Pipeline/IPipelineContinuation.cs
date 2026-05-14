using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for the next-step continuation passed to <see cref="IPipelineBehavior{TRequest,TResponse}.HandleAsync{TNext}"/>.
/// </summary>
/// <remarks>
/// Implementations are mutable struct-based "walkers" supplied by the mediator infrastructure - callers should
/// only invoke <see cref="InvokeAsync"/>. Constrain the generic next-parameter as
/// <c>where TNext : struct, IPipelineContinuation&lt;TRequest, TResponse&gt;</c> to let the JIT specialize the
/// pipeline-behavior method and devirtualize the continuation call without allocating a delegate.
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
    ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken);
}

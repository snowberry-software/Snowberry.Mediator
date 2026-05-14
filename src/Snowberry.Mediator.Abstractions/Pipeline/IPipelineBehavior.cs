using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for a pipeline behavior. Each behavior receives a struct continuation that, when invoked,
/// advances to the next behavior in the chain (or the terminal handler).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request and forwards to <paramref name="next"/>, optionally adding behavior-specific logic.
    /// </summary>
    /// <typeparam name="TNext">The struct continuation type - the JIT specializes the method per
    /// continuation type so that <c>next.InvokeAsync(...)</c> is a direct call with no delegate or boxing.</typeparam>
    /// <param name="request">The request.</param>
    /// <param name="next">The continuation to the next step in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<TResponse> HandleAsync<TNext>(
        TRequest request,
        TNext next,
        CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>;
}
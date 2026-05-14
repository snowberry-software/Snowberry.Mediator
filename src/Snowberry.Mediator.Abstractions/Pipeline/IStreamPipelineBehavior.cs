using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for a stream pipeline behavior. Each behavior receives a struct continuation that, when
/// invoked, advances to the next behavior in the chain (or to the terminal stream request handler).
/// </summary>
/// <typeparam name="TRequest">The stream request type.</typeparam>
/// <typeparam name="TResponse">The response element type produced by the stream.</typeparam>
public interface IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    /// <summary>
    /// Handles the request and forwards to <paramref name="next"/>, optionally adding behavior-specific
    /// stream transformations.
    /// </summary>
    /// <typeparam name="TNext">The struct continuation type — the JIT specializes the method per
    /// continuation type so that <c>next.InvokeAsync(...)</c> is a direct call with no delegate or boxing.</typeparam>
    /// <param name="request">The stream request.</param>
    /// <param name="next">The continuation to the next step in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> HandleAsync<TNext>(
        TRequest request,
        TNext next,
        CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>;
}

using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for the next-step continuation passed to <see cref="IStreamPipelineBehavior{TRequest,TResponse}.HandleAsync{TNext}"/>.
/// </summary>
/// <remarks>
/// Implementations are supplied by the mediator infrastructure; callers should only invoke <see cref="InvokeAsync"/>.
/// </remarks>
/// <typeparam name="TRequest">The stream request type.</typeparam>
/// <typeparam name="TResponse">The response element type produced by the stream.</typeparam>
public interface IStreamPipelineContinuation<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    /// <summary>
    /// Invokes the next step in the stream pipeline. If this is the final continuation in the chain, the
    /// terminal <see cref="Handler.IStreamRequestHandler{TRequest,TResponse}"/> is invoked.
    /// </summary>
    /// <param name="request">The stream request.</param>
    /// <param name="cancellationToken">The cancellation token. Behaviors that annotate their own
    /// <see cref="CancellationToken"/> parameter with <see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute"/>
    /// receive this token as the enumerator-construction token.</param>
    /// <returns>An asynchronous stream of <typeparamref name="TResponse"/>.</returns>
    IAsyncEnumerable<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken);
}
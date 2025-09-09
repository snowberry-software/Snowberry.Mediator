using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for a stream pipeline behavior in a linked delegate chain. Each behavior MUST have a non-null <see cref="NextPipeline"/> delegate.
/// The final behavior's <see cref="NextPipeline"/> points to the terminal stream request handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    /// <summary>
    /// The next delegate in the stream pipeline chain. Always non-null once execution begins.
    /// </summary>
    StreamPipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; }

    /// <summary>
    /// Handles the request and forwards to <see cref="NextPipeline"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    IAsyncEnumerable<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}

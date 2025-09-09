using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Pipeline;

/// <summary>
/// Contract for a pipeline behavior in a linked delegate chain. Each behavior MUST have a non-null <see cref="NextPipeline"/> delegate.
/// The final behavior's <see cref="NextPipeline"/> points to the terminal request handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <summary>
    /// The next delegate in the pipeline chain. Always non-null once execution begins.
    /// </summary>
    PipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; }

    /// <summary>
    /// Handles the request and forwards to <see cref="NextPipeline"/>, optionally adding behavior-specific logic.
    /// </summary>
    ValueTask<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}

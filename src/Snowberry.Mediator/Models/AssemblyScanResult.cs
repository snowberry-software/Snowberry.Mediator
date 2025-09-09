namespace Snowberry.Mediator.Models;

/// <summary>
/// The result of scanning an assembly for mediator types.
/// </summary>
public class AssemblyScanResult
{
    /// <summary>
    /// The collection of all request types.
    /// </summary>
    public IReadOnlyList<Type>? RequestTypes { get; init; }

    /// <summary>
    /// The collection of all request handler types.
    /// </summary>
    public IReadOnlyList<RequestHandlerInfo>? RequestHandlerTypes { get; init; }

    /// <summary>
    /// The collection of all stream request types.
    /// </summary>
    public IReadOnlyList<Type>? StreamRequestTypes { get; init; }

    /// <summary>
    /// The collection of all stream request handler types.
    /// </summary>
    public IReadOnlyList<StreamRequestHandlerInfo>? StreamRequestHandlerTypes { get; init; }

    /// <summary>
    /// The collection of all notification types.
    /// </summary>
    public IReadOnlyList<Type>? NotificationTypes { get; init; }

    /// <summary>
    /// The collection of all notification handler types.
    /// </summary>
    public IReadOnlyList<NotificationHandlerInfo>? NotificationHandlerTypes { get; init; }

    /// <summary>
    /// The collection of all pipeline behavior types.
    /// </summary>
    public IReadOnlyList<PipelineBehaviorHandlerInfo>? PipelineBehaviorTypes { get; init; }

    /// <summary>
    /// The collection of all stream pipeline behavior types.
    /// </summary>
    public IReadOnlyList<StreamPipelineBehaviorHandlerInfo>? StreamPipelineBehaviorTypes { get; init; }
}

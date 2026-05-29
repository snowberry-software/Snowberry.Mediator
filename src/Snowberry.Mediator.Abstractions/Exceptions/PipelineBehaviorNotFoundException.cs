namespace Snowberry.Mediator.Abstractions.Exceptions;

/// <summary>
/// Gets thrown when no pipeline behavior is found for a given request type.
/// </summary>
/// <param name="requestType">The request type.</param>
/// <param name="isStream">Specifies whether the request type is a stream request.</param>
public class PipelineBehaviorNotFoundException(Type requestType, bool isStream) : Exception($"Pipeline behavior found for request type: {requestType.FullName}.")
{
    /// <summary>
    /// Gets a value indicating whether the request is a stream request.
    /// </summary>
    public bool IsStream { get; } = isStream;

    /// <summary>
    /// The request type.
    /// </summary>
    public Type RequestType { get; } = requestType;
}
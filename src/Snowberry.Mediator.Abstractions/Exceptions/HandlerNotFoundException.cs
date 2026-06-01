namespace Snowberry.Mediator.Abstractions.Exceptions;

/// <summary>
/// Gets thrown when no handler is found for a given request type.
/// </summary>
/// <param name="requestType">The request type that has no associated handler.</param>
/// <param name="isStream">A value indicating whether the request is a stream request.</param>
public class HandlerNotFoundException(Type requestType, bool isStream) : Exception($"No handler found for request type: {requestType.FullName}.")
{
    /// <summary>
    /// Gets a value indicating whether the request is a stream request.
    /// </summary>
    public bool IsStream { get; } = isStream;

    /// <summary>
    /// Gets the request type that has no associated handler.
    /// </summary>
    public Type RequestType { get; } = requestType;
}
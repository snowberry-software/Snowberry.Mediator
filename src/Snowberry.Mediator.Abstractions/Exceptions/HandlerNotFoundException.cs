namespace Snowberry.Mediator.Abstractions.Exceptions;

/// <summary>
/// Gets thrown when no handler is found for a given request type.
/// </summary>
public class HandlerNotFoundException(Type requestType, bool isStream) : Exception($"No handler found for request type: {requestType.FullName}.")
{
    /// <summary>
    /// Specifies the request type that has no associated stream handler.
    /// </summary>
    public bool IsStream { get; } = isStream;
}

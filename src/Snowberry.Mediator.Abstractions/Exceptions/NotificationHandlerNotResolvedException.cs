namespace Snowberry.Mediator.Abstractions.Exceptions;

/// <summary>
/// Gets thrown when no notification handler is found for a given notification type.
/// </summary>
/// <param name="notificationType">The notification type.</param>
public class NotificationHandlerNotResolvedException(Type notificationType) : Exception($"No notification handler found for notification type: {notificationType.FullName}.")
{
    /// <summary>
    /// Specifies the notification type that has no associated handler.
    /// </summary>
    public Type NotificationType { get; } = notificationType;
}

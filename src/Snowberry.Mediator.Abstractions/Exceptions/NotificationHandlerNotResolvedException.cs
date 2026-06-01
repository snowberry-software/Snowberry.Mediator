namespace Snowberry.Mediator.Abstractions.Exceptions;

/// <summary>
/// Gets thrown when a notification handler cannot be resolved while publishing a notification, either because no
/// handler is registered for the notification type or because a registered handler could not be obtained from the
/// service provider.
/// </summary>
public class NotificationHandlerNotResolvedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHandlerNotResolvedException"/> class for a
    /// notification type that has no resolvable handler.
    /// </summary>
    /// <param name="notificationType">The notification type for which no handler could be resolved.</param>
    public NotificationHandlerNotResolvedException(Type notificationType)
        : base($"No notification handler found for notification type: {notificationType.FullName}.")
    {
        NotificationType = notificationType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHandlerNotResolvedException"/> class for a
    /// registered handler that could not be obtained from the service provider.
    /// </summary>
    /// <param name="notificationType">The notification type being published.</param>
    /// <param name="handlerType">The handler type that could not be resolved from the service provider.</param>
    public NotificationHandlerNotResolvedException(Type notificationType, Type handlerType)
        : base($"The notification handler '{handlerType.FullName}' for notification type '{notificationType.FullName}' could not be resolved from the service provider.")
    {
        NotificationType = notificationType;
        HandlerType = handlerType;
    }

    /// <summary>
    /// Gets the notification type that was being published when the handler could not be resolved.
    /// </summary>
    public Type NotificationType { get; }

    /// <summary>
    /// Gets the handler type that could not be resolved from the service provider, or <see langword="null"/> when
    /// no handler is registered for <see cref="NotificationType"/>.
    /// </summary>
    public Type? HandlerType { get; }
}

using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;

namespace Snowberry.Mediator.Registries.Contracts;

/// <summary>
/// The contract for a notification handler registry for a specific notification type.
/// </summary>
/// <typeparam name="TNotificationHandlerInfo">The notification handler information type.</typeparam>
public interface IGlobalNotificationHandlerRegistry<TNotificationHandlerInfo>
    where TNotificationHandlerInfo : NotificationHandlerInfo
{
    /// <summary>
    /// Registers a notification handler.
    /// </summary>
    /// <param name="handlerInfo">The notification handler info.</param>
    void Register(TNotificationHandlerInfo handlerInfo);

    /// <summary>
    /// Publishes the notification to all registered handlers.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask PublishAsync<TNotification>(IServiceProvider serviceProvider, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;

    /// <summary>
    /// Gets whether the registry is empty.
    /// </summary>
    bool IsEmpty { get; }
}
using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.Abstractions.Handler;

/// <summary>
/// The contract for handling notifications.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}

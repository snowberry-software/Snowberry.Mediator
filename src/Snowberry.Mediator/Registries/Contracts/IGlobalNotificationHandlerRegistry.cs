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
    /// Builds the read-optimized snapshot of registered handlers. Called once after all registrations
    /// are complete. Subsequent registrations are picked up by the next <see cref="Build"/> call.
    /// </summary>
    void Build();

    /// <summary>
    /// Publishes the notification to all registered handlers.
    /// </summary>
    /// <typeparam name="TNotification">The notification type being published.</typeparam>
    /// <param name="serviceProvider">The service provider used to resolve the notification handlers.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when every matching handler has finished handling the notification.</returns>
    ValueTask PublishAsync<TNotification>(IServiceProvider serviceProvider, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;

    /// <summary>
    /// Registers a notification handler.
    /// </summary>
    /// <param name="handlerInfo">The notification handler info.</param>
    void Register(TNotificationHandlerInfo handlerInfo);

    /// <summary>
    /// Gets whether the registry is empty.
    /// </summary>
    bool IsEmpty { get; }
}
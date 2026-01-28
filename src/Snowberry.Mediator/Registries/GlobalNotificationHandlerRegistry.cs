using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;
using ZLinq;

namespace Snowberry.Mediator.Registries;

/// <inheritdoc cref="IGlobalNotificationHandlerRegistry{TNotificationHandlerInfo}"/>
public class GlobalNotificationHandlerRegistry : IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>
{
#if NET9_0_OR_GREATER
    protected readonly Lock _lock = new();
#else
    protected readonly object _lock = new();
#endif
    protected Dictionary<Type, List<NotificationHandlerInfo>> _notificationHandlers = [];
    protected List<NotificationHandlerInfo> _openGenericHandlers = [];

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Notification handlers are explicitly registered, not discovered through reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Notification handlers are explicitly registered, not discovered through reflection.")]
    public async ValueTask PublishAsync<TNotification>(IServiceProvider serviceProvider, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (IsEmpty)
            throw new NotificationHandlerNotResolvedException(typeof(TNotification));

        bool hadHandler = false;
        if (_openGenericHandlers.Count > 0)
        {
            var notificationType = typeof(TNotification);

            for (int i = 0; i < _openGenericHandlers.Count; i++)
            {
                var openGenericHandlerInfo = _openGenericHandlers[i];
                var current = Unsafe.As<INotificationHandler<TNotification>>(serviceProvider.GetService(openGenericHandlerInfo.HandlerType.MakeGenericType(notificationType)))
                    ?? throw new NotificationHandlerNotResolvedException(openGenericHandlerInfo.HandlerType);

                hadHandler = true;
                var valueTask = current.HandleAsync(notification, cancellationToken);

                if (!valueTask.IsCompletedSuccessfully)
                    await valueTask;
            }
        }

        if (_notificationHandlers.TryGetValue(typeof(TNotification), out var notificationHandlers))
        {
            for (int i = 0; i < notificationHandlers.Count; i++)
            {
                var notificationHandlerInfo = notificationHandlers[i];
                var current = Unsafe.As<INotificationHandler<TNotification>>(serviceProvider.GetService(notificationHandlerInfo.HandlerType)
                    ?? throw new NotificationHandlerNotResolvedException(notificationHandlerInfo.HandlerType));

                hadHandler = true;
                var valueTask = current.HandleAsync(notification, cancellationToken);

                if (!valueTask.IsCompletedSuccessfully)
                    await valueTask;
            }
        }

        if (!hadHandler)
            throw new NotificationHandlerNotResolvedException(typeof(TNotification));
    }

    /// <inheritdoc/>
    public void Register(NotificationHandlerInfo handlerInfo)
    {
        lock (_lock)
        {
            if (handlerInfo.HandlerType.IsGenericTypeDefinition)
            {
                if (!_openGenericHandlers.Contains(handlerInfo))
                    _openGenericHandlers.Add(handlerInfo);

                return;
            }

            if (!_notificationHandlers.TryGetValue(handlerInfo.NotificationType, out var notificationHandlers))
                _notificationHandlers.Add(handlerInfo.NotificationType, notificationHandlers = []);

            if (!notificationHandlers.AsValueEnumerable().Any(x => x == handlerInfo))
                notificationHandlers.Add(handlerInfo);
        }
    }

    /// <inheritdoc/>
    public bool IsEmpty
    {
        get
        {
            lock (_lock)
            {
                return _notificationHandlers.Count == 0 && _openGenericHandlers.Count == 0;
            }
        }
    }
}

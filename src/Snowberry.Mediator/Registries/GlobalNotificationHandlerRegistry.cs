using System.Collections.Concurrent;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Default <see cref="IGlobalNotificationHandlerRegistry{TNotificationHandlerInfo}"/> implementation.
/// Tracks both concrete and open-generic
/// <see cref="INotificationHandler{TNotification}"/> registrations and dispatches
/// notifications to every matching handler in registration order, resolving each handler from the supplied
/// <see cref="IServiceProvider"/> on every publish.
/// </summary>
public sealed class GlobalNotificationHandlerRegistry : IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    // Mutable registration state (writes under _lock).
    private readonly Dictionary<Type, List<NotificationHandlerInfo>> _notificationHandlers = [];
    private readonly List<NotificationHandlerInfo> _openGenericHandlers = [];

    // Read-optimized frozen snapshots.
#if NET8_0_OR_GREATER
    private FrozenDictionary<Type, NotificationHandlerInfo[]> _frozenNotificationHandlers
        = FrozenDictionary<Type, NotificationHandlerInfo[]>.Empty;
#else
    private Dictionary<Type, NotificationHandlerInfo[]> _frozenNotificationHandlers = [];
#endif
    private NotificationHandlerInfo[] _frozenOpenGenericHandlers = [];

    // Per-notification-type cache of pre-closed open-generic handler types. First Publish for a
    // notification type populates; cleared in Build() under _lock when registrations change.
    // Eliminates the per-call MakeGenericType allocations (a Type[1] arg array + scratch per handler).
    private readonly ConcurrentDictionary<Type, Type[]> _closedOpenGenericTypeCache = new();

    private volatile bool _isEmpty = true;
    private volatile bool _dirty = false;

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Notification handlers are explicitly registered, not discovered through reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Notification handlers are explicitly registered, not discovered through reflection.")]
    public async ValueTask PublishAsync<TNotification>(IServiceProvider serviceProvider, TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_isEmpty)
            throw new NotificationHandlerNotResolvedException(typeof(TNotification));

        if (_dirty)
            Build();

        bool hadHandler = false;
        var openGeneric = _frozenOpenGenericHandlers;
        if (openGeneric.Length > 0)
        {
            var notificationType = typeof(TNotification);
            if (!_closedOpenGenericTypeCache.TryGetValue(notificationType, out var closedTypes)
                || closedTypes.Length != openGeneric.Length)
            {
                closedTypes = new Type[openGeneric.Length];
                for (int i = 0; i < openGeneric.Length; i++)
                    closedTypes[i] = openGeneric[i].HandlerType.MakeGenericType(notificationType);
                _closedOpenGenericTypeCache[notificationType] = closedTypes;
            }

            for (int i = 0; i < openGeneric.Length; i++)
            {
                var info = openGeneric[i];
                var current = Unsafe.As<INotificationHandler<TNotification>>(serviceProvider.GetService(closedTypes[i]))
                    ?? throw new NotificationHandlerNotResolvedException(info.HandlerType);

                hadHandler = true;
                if (!MediatorDiagnostics.IsNotificationEnabled)
                {
                    var valueTask = current.HandleAsync(notification, cancellationToken);
                    if (!valueTask.IsCompletedSuccessfully)
                        await valueTask;
                }
                else
                {
                    await InvokeHandlerInstrumentedAsync(current, notification, info.HandlerType, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (_frozenNotificationHandlers.TryGetValue(typeof(TNotification), out var specific))
        {
            for (int i = 0; i < specific.Length; i++)
            {
                var info = specific[i];
                var current = Unsafe.As<INotificationHandler<TNotification>>(serviceProvider.GetService(info.HandlerType)
                    ?? throw new NotificationHandlerNotResolvedException(info.HandlerType));

                hadHandler = true;
                if (!MediatorDiagnostics.IsNotificationEnabled)
                {
                    var valueTask = current.HandleAsync(notification, cancellationToken);
                    if (!valueTask.IsCompletedSuccessfully)
                        await valueTask;
                }
                else
                {
                    await InvokeHandlerInstrumentedAsync(current, notification, info.HandlerType, cancellationToken).ConfigureAwait(false);
                }
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
                {
                    _openGenericHandlers.Add(handlerInfo);
                    _isEmpty = false;
                    _dirty = true;
                }
                return;
            }

            if (!_notificationHandlers.TryGetValue(handlerInfo.NotificationType, out var notificationHandlers))
                _notificationHandlers.Add(handlerInfo.NotificationType, notificationHandlers = []);

            if (!notificationHandlers.Contains(handlerInfo))
            {
                notificationHandlers.Add(handlerInfo);
                _isEmpty = false;
                _dirty = true;
            }
        }
    }

    /// <inheritdoc/>
    public void Build()
    {
        lock (_lock)
        {
            var openArr = _openGenericHandlers.ToArray();

            var dict = new Dictionary<Type, NotificationHandlerInfo[]>(_notificationHandlers.Count);
            foreach (var kvp in _notificationHandlers)
            {
                dict[kvp.Key] = kvp.Value.ToArray();
            }

#if NET8_0_OR_GREATER
            _frozenNotificationHandlers = dict.ToFrozenDictionary();
#else
            _frozenNotificationHandlers = dict;
#endif
            _frozenOpenGenericHandlers = openArr;
            _closedOpenGenericTypeCache.Clear();
            _dirty = false;
        }
    }

    /// <inheritdoc/>
    public bool IsEmpty => _isEmpty;

    private static async ValueTask InvokeHandlerInstrumentedAsync<TNotification>(
        INotificationHandler<TNotification> handler,
        TNotification notification,
        Type handlerType,
        CancellationToken ct)
        where TNotification : INotification
    {
        using var activity = MediatorDiagnostics.s_NotificationSource.StartActivity(
            "Mediator.Handler " + handlerType.Name, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("snowberry.mediator.handler.type", handlerType.Name);
            activity.SetTag("snowberry.mediator.notification.type", typeof(TNotification).Name);
        }
        try
        {
            await handler.HandleAsync(notification, ct).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
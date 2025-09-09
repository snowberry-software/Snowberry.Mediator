
using Snowberry.Mediator.Abstractions.Handler;

namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a notification handler.
/// </summary>
public class NotificationHandlerInfo : IEquatable<NotificationHandlerInfo>
{
    public static IList<NotificationHandlerInfo>? TryParse(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return default;

        var expectedInterface = typeof(INotificationHandler<>);

        var interfaces = type.GetInterfaces();

        var results = new List<NotificationHandlerInfo>();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var inter = interfaces[i];

            if (!inter.IsGenericType)
                continue;

            var def = inter.GetGenericTypeDefinition();

            if (def == expectedInterface)
            {
                var genericArguments = inter.GetGenericArguments();
                var notificationType = genericArguments[0];

                results.Add(new NotificationHandlerInfo()
                {
                    HandlerType = type,
                    NotificationType = notificationType
                });
            }
        }

        return results;
    }

    /// <summary>
    /// The notification type.
    /// </summary>
    public required Type NotificationType { get; init; }

    /// <summary>
    /// The handler type.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is NotificationHandlerInfo info && Equals(info);
    }

    /// <inheritdoc/>
    public bool Equals(NotificationHandlerInfo? other)
    {
        return other is NotificationHandlerInfo info &&
               EqualityComparer<Type>.Default.Equals(NotificationType, info.NotificationType) &&
               EqualityComparer<Type>.Default.Equals(HandlerType, info.HandlerType);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(NotificationType, HandlerType);
    }
}
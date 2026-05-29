using System.Diagnostics.CodeAnalysis;
using Snowberry.Mediator.Abstractions.Handler;

namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a notification handler.
/// </summary>
public class NotificationHandlerInfo : IEquatable<NotificationHandlerInfo>
{
    /// <summary>
    /// Parses a handler type into one <see cref="NotificationHandlerInfo"/> per
    /// <see cref="INotificationHandler{TNotification}"/> implementation it declares.
    /// </summary>
    /// <param name="type">The handler type to inspect.</param>
    /// <returns>The parsed handler-info entries, or <see langword="null"/> if <paramref name="type"/> is abstract or an interface.</returns>
    public static IList<NotificationHandlerInfo>? TryParse([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
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
#if NET9_0_OR_GREATER
        return HashCode.Combine(NotificationType, HandlerType);
#else
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (NotificationType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (HandlerType?.GetHashCode() ?? 0);
            return hash;
        }
#endif
    }

    /// <summary>
    /// The handler type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type HandlerType { get; init; }

    /// <summary>
    /// The notification type.
    /// </summary>
    public required Type NotificationType { get; init; }
}
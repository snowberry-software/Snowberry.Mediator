using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests.Common.NotificationHandlers;

/// <summary>
/// Simple notification handler for testing
/// </summary>
public class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public static List<SimpleNotification> ReceivedNotifications { get; } = [];

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SimpleNotificationHandler));
        ReceivedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }

    public static void ClearReceivedNotifications()
    {
        ReceivedNotifications.Clear();
    }
}

/// <summary>
/// Another handler for the same notification to test multiple handlers
/// </summary>
public class AnotherSimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public static int ExecutionCount { get; private set; }

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(AnotherSimpleNotificationHandler));
        ExecutionCount++;
        return ValueTask.CompletedTask;
    }

    public static void ResetExecutionCount()
    {
        ExecutionCount = 0;
    }
}

/// <summary>
/// User registration handler for testing domain-specific notifications
/// </summary>
public class UserRegisteredNotificationHandler : INotificationHandler<UserRegisteredNotification>
{
    public static List<UserRegisteredNotification> ProcessedUsers { get; } = [];

    public async ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegisteredNotificationHandler));

        // Simulate some async work
        await Task.Delay(1, cancellationToken);

        ProcessedUsers.Add(notification);
    }

    public static void ClearProcessedUsers()
    {
        ProcessedUsers.Clear();
    }
}

/// <summary>
/// Email notification handler for user registrations
/// </summary>
public class UserRegistrationEmailHandler : INotificationHandler<UserRegisteredNotification>
{
    public static List<string> EmailsSent { get; } = [];

    public ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegistrationEmailHandler));
        EmailsSent.Add($"Welcome email sent to {notification.Email}");
        return ValueTask.CompletedTask;
    }

    public static void ClearEmailsSent()
    {
        EmailsSent.Clear();
    }
}

/// <summary>
/// Order completion handler for testing business logic
/// </summary>
public class OrderCompletionHandler : INotificationHandler<OrderCompletedNotification>
{
    public static List<OrderCompletedNotification> CompletedOrders { get; } = [];
    public static decimal TotalRevenue { get; private set; }

    public ValueTask HandleAsync(OrderCompletedNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(OrderCompletionHandler));
        CompletedOrders.Add(notification);
        TotalRevenue += notification.Amount;
        return ValueTask.CompletedTask;
    }

    public static void Reset()
    {
        CompletedOrders.Clear();
        TotalRevenue = 0;
    }
}

/// <summary>
/// System event logging handler
/// </summary>
public class SystemEventLoggingHandler : INotificationHandler<SystemEventNotification>
{
    public static List<SystemEventNotification> LoggedEvents { get; } = [];

    public ValueTask HandleAsync(SystemEventNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SystemEventLoggingHandler));
        LoggedEvents.Add(notification);
        return ValueTask.CompletedTask;
    }

    public static void ClearLoggedEvents()
    {
        LoggedEvents.Clear();
    }
}
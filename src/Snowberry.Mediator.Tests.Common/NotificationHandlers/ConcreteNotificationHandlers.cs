using System.Collections.Concurrent;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests.Common.NotificationHandlers;

/// <summary>
/// Simple notification handler for testing
/// </summary>
public class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public static void ClearReceivedNotifications()
    {
        // Clear by draining the bag
        var bag = ReceivedNotifications;
        while (bag.TryTake(out _))
        {
        }
    }

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SimpleNotificationHandler));
        ReceivedNotifications.Add(notification);
        return default;
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<SimpleNotification> ReceivedNotifications =>
        TestIsolationContext.GetOrCreateBag<SimpleNotification>("SimpleNotificationHandler.ReceivedNotifications");
}

/// <summary>
/// Another handler for the same notification to test multiple handlers
/// </summary>
public class AnotherSimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    public static void ResetExecutionCount()
    {
        TestIsolationContext.GetOrSetValue(ExecutionCountKey, 0);
    }

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(AnotherSimpleNotificationHandler));
        // Thread-safe increment in isolated context
        int current = TestIsolationContext.GetValue(ExecutionCountKey, 0);
        TestIsolationContext.GetOrSetValue(ExecutionCountKey, current + 1);
        return default;
    }

    // Use thread-safe property access with test isolation
    public static int ExecutionCount => TestIsolationContext.GetValue(ExecutionCountKey, 0);
    private static string ExecutionCountKey => "AnotherSimpleNotificationHandler.ExecutionCount";
}

/// <summary>
/// User registration handler for testing domain-specific notifications
/// </summary>
public class UserRegisteredNotificationHandler : INotificationHandler<UserRegisteredNotification>
{
    public static void ClearProcessedUsers()
    {
        // Clear by draining the bag
        var bag = ProcessedUsers;
        while (bag.TryTake(out _))
        {
        }
    }

    public async ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegisteredNotificationHandler));

        // Simulate some async work
        await Task.Delay(1, cancellationToken);

        ProcessedUsers.Add(notification);
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<UserRegisteredNotification> ProcessedUsers =>
        TestIsolationContext.GetOrCreateBag<UserRegisteredNotification>("UserRegisteredNotificationHandler.ProcessedUsers");
}

/// <summary>
/// Email notification handler for user registrations
/// </summary>
public class UserRegistrationEmailHandler : INotificationHandler<UserRegisteredNotification>
{
    public static void ClearEmailsSent()
    {
        // Clear by draining the bag
        var bag = EmailsSent;
        while (bag.TryTake(out _))
        {
        }
    }

    public ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegistrationEmailHandler));
        EmailsSent.Add($"Welcome email sent to {notification.Email}");
        return default;
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<string> EmailsSent =>
        TestIsolationContext.GetOrCreateBag<string>("UserRegistrationEmailHandler.EmailsSent");
}

/// <summary>
/// Order completion handler for testing business logic
/// </summary>
public class OrderCompletionHandler : INotificationHandler<OrderCompletedNotification>
{
    public static void Reset()
    {
        // Clear by draining the bag
        var bag = CompletedOrders;
        while (bag.TryTake(out _))
        {
        }

        TestIsolationContext.GetOrSetValue(TotalRevenueKey, 0m);
    }

    public ValueTask HandleAsync(OrderCompletedNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(OrderCompletionHandler));
        CompletedOrders.Add(notification);

        // Thread-safe addition using test isolation context
        decimal currentRevenue = TestIsolationContext.GetValue(TotalRevenueKey, 0m);
        TestIsolationContext.GetOrSetValue(TotalRevenueKey, currentRevenue + notification.Amount);

        return default;
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<OrderCompletedNotification> CompletedOrders =>
        TestIsolationContext.GetOrCreateBag<OrderCompletedNotification>(CompletedOrdersKey);

    public static decimal TotalRevenue => TestIsolationContext.GetValue(TotalRevenueKey, 0m);
    private static string CompletedOrdersKey => "OrderCompletionHandler.CompletedOrders";
    private static string TotalRevenueKey => "OrderCompletionHandler.TotalRevenue";
}

/// <summary>
/// System event logging handler
/// </summary>
public class SystemEventLoggingHandler : INotificationHandler<SystemEventNotification>
{
    public static void ClearLoggedEvents()
    {
        // Clear by draining the bag
        var bag = LoggedEvents;
        while (bag.TryTake(out _))
        {
        }
    }

    public ValueTask HandleAsync(SystemEventNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SystemEventLoggingHandler));
        LoggedEvents.Add(notification);
        return default;
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<SystemEventNotification> LoggedEvents =>
        TestIsolationContext.GetOrCreateBag<SystemEventNotification>("SystemEventLoggingHandler.LoggedEvents");
}
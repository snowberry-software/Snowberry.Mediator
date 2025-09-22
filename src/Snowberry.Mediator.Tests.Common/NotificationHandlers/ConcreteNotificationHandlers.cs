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
    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<SimpleNotification> ReceivedNotifications =>
        TestIsolationContext.GetOrCreateBag<SimpleNotification>("SimpleNotificationHandler.ReceivedNotifications");

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SimpleNotificationHandler));
        ReceivedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }

    public static void ClearReceivedNotifications()
    {
        // Clear by draining the bag
        var bag = ReceivedNotifications;
        while (bag.TryTake(out _))
        {
        }
    }
}

/// <summary>
/// Another handler for the same notification to test multiple handlers
/// </summary>
public class AnotherSimpleNotificationHandler : INotificationHandler<SimpleNotification>
{
    private static string ExecutionCountKey => "AnotherSimpleNotificationHandler.ExecutionCount";

    // Use thread-safe property access with test isolation
    public static int ExecutionCount => TestIsolationContext.GetValue(ExecutionCountKey, 0);

    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(AnotherSimpleNotificationHandler));
        // Thread-safe increment in isolated context
        int current = TestIsolationContext.GetValue(ExecutionCountKey, 0);
        TestIsolationContext.GetOrSetValue(ExecutionCountKey, current + 1);
        return ValueTask.CompletedTask;
    }

    public static void ResetExecutionCount()
    {
        TestIsolationContext.GetOrSetValue(ExecutionCountKey, 0);
    }
}

/// <summary>
/// User registration handler for testing domain-specific notifications
/// </summary>
public class UserRegisteredNotificationHandler : INotificationHandler<UserRegisteredNotification>
{
    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<UserRegisteredNotification> ProcessedUsers =>
        TestIsolationContext.GetOrCreateBag<UserRegisteredNotification>("UserRegisteredNotificationHandler.ProcessedUsers");

    public async ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegisteredNotificationHandler));

        // Simulate some async work
        await Task.Delay(1, cancellationToken);

        ProcessedUsers.Add(notification);
    }

    public static void ClearProcessedUsers()
    {
        // Clear by draining the bag
        var bag = ProcessedUsers;
        while (bag.TryTake(out _))
        {
        }
    }
}

/// <summary>
/// Email notification handler for user registrations
/// </summary>
public class UserRegistrationEmailHandler : INotificationHandler<UserRegisteredNotification>
{
    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<string> EmailsSent =>
        TestIsolationContext.GetOrCreateBag<string>("UserRegistrationEmailHandler.EmailsSent");

    public ValueTask HandleAsync(UserRegisteredNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(UserRegistrationEmailHandler));
        EmailsSent.Add($"Welcome email sent to {notification.Email}");
        return ValueTask.CompletedTask;
    }

    public static void ClearEmailsSent()
    {
        // Clear by draining the bag
        var bag = EmailsSent;
        while (bag.TryTake(out _))
        {
        }
    }
}

/// <summary>
/// Order completion handler for testing business logic
/// </summary>
public class OrderCompletionHandler : INotificationHandler<OrderCompletedNotification>
{
    private static string CompletedOrdersKey => "OrderCompletionHandler.CompletedOrders";
    private static string TotalRevenueKey => "OrderCompletionHandler.TotalRevenue";

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<OrderCompletedNotification> CompletedOrders =>
        TestIsolationContext.GetOrCreateBag<OrderCompletedNotification>(CompletedOrdersKey);

    public static decimal TotalRevenue => TestIsolationContext.GetValue(TotalRevenueKey, 0m);

    public ValueTask HandleAsync(OrderCompletedNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(OrderCompletionHandler));
        CompletedOrders.Add(notification);

        // Thread-safe addition using test isolation context
        decimal currentRevenue = TestIsolationContext.GetValue(TotalRevenueKey, 0m);
        TestIsolationContext.GetOrSetValue(TotalRevenueKey, currentRevenue + notification.Amount);

        return ValueTask.CompletedTask;
    }

    public static void Reset()
    {
        // Clear by draining the bag
        var bag = CompletedOrders;
        while (bag.TryTake(out _))
        {
        }

        TestIsolationContext.GetOrSetValue(TotalRevenueKey, 0m);
    }
}

/// <summary>
/// System event logging handler
/// </summary>
public class SystemEventLoggingHandler : INotificationHandler<SystemEventNotification>
{
    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<SystemEventNotification> LoggedEvents =>
        TestIsolationContext.GetOrCreateBag<SystemEventNotification>("SystemEventLoggingHandler.LoggedEvents");

    public ValueTask HandleAsync(SystemEventNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution(nameof(SystemEventLoggingHandler));
        LoggedEvents.Add(notification);
        return ValueTask.CompletedTask;
    }

    public static void ClearLoggedEvents()
    {
        // Clear by draining the bag
        var bag = LoggedEvents;
        while (bag.TryTake(out _))
        {
        }
    }
}
using System.Collections.Concurrent;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.NotificationHandlers;

/// <summary>
/// Generic logging handler that handles any notification
/// </summary>
public class GenericLoggingHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public static void ClearLoggedNotifications()
    {
        // Clear by draining the bag
        var bag = LoggedNotifications;
        while (bag.TryTake(out _))
        {
        }
    }

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericLoggingHandler<{typeof(TNotification).Name}>");
        LoggedNotifications.Add(notification);
        return default;
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<object> LoggedNotifications =>
        TestIsolationContext.GetOrCreateBag<object>($"GenericLoggingHandler<{typeof(TNotification).Name}>.LoggedNotifications");
}

/// <summary>
/// Generic auditing handler that records all notifications for compliance
/// </summary>
public class GenericAuditingHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public static void ClearAuditLog()
    {
        var auditLog = AuditLog;
        auditLog.Clear();
    }

    public async ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericAuditingHandler<{typeof(TNotification).Name}>");

        // Simulate async audit processing
        await Task.Delay(1, cancellationToken);

        string notificationType = typeof(TNotification).Name;
        var auditBag = AuditLog.GetOrAdd(notificationType, _ => []);
        auditBag.Add(notification);
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentDictionary<string, ConcurrentBag<object>> AuditLog =>
        TestIsolationContext.GetOrCreateValue(AuditLogKey, () => new ConcurrentDictionary<string, ConcurrentBag<object>>());

    private static string AuditLogKey => $"GenericAuditingHandler<{typeof(TNotification).Name}>.AuditLog";
}

/// <summary>
/// Generic metrics handler that collects statistics on all notifications
/// </summary>
public class GenericMetricsHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public static void ClearMetrics()
    {
        NotificationCounts.Clear();
        LastProcessedTimes.Clear();
    }

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericMetricsHandler<{typeof(TNotification).Name}>");

        string notificationType = typeof(TNotification).Name;

        // Thread-safe increment
        NotificationCounts.AddOrUpdate(notificationType, 1, (key, value) => value + 1);
        LastProcessedTimes[notificationType] = DateTime.UtcNow;

        return default;
    }

    public static ConcurrentDictionary<string, DateTime> LastProcessedTimes =>
        TestIsolationContext.GetOrCreateValue(LastProcessedTimesKey, () => new ConcurrentDictionary<string, DateTime>());

    // Use test-isolated state instead of shared static state
    public static ConcurrentDictionary<string, int> NotificationCounts =>
        TestIsolationContext.GetOrCreateValue(NotificationCountsKey, () => new ConcurrentDictionary<string, int>());

    private static string LastProcessedTimesKey => $"GenericMetricsHandler<{typeof(TNotification).Name}>.LastProcessedTimes";
    private static string NotificationCountsKey => $"GenericMetricsHandler<{typeof(TNotification).Name}>.NotificationCounts";
}

/// <summary>
/// Generic validation handler that could perform validation on any notification
/// </summary>
public class GenericValidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public static void ClearValidationResults()
    {
        // Clear by draining the bag
        var bag = ValidationResults;
        while (bag.TryTake(out _))
        {
        }

        ShouldThrowException = false;
    }

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericValidationHandler<{typeof(TNotification).Name}>");

        if (ShouldThrowException)
        {
            throw new InvalidOperationException($"Validation failed for {typeof(TNotification).Name}");
        }

        ValidationResults.Add($"Validated {typeof(TNotification).Name} successfully");
        return default;
    }

    public static bool ShouldThrowException
    {
        get => TestIsolationContext.GetValue(ShouldThrowExceptionKey, false);
        set => TestIsolationContext.GetOrSetValue(ShouldThrowExceptionKey, value);
    }

    // Use test-isolated state instead of shared static state
    public static ConcurrentBag<string> ValidationResults =>
        TestIsolationContext.GetOrCreateBag<string>(ValidationResultsKey);

    private static string ShouldThrowExceptionKey => $"GenericValidationHandler<{typeof(TNotification).Name}>.ShouldThrowException";
    private static string ValidationResultsKey => $"GenericValidationHandler<{typeof(TNotification).Name}>.ValidationResults";
}

/// <summary>
/// Test-specific validation handler that won't interfere with other tests
/// Uses AsyncLocal for test isolation to prevent race conditions
/// </summary>
public class TestSpecificValidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    // Use AsyncLocal to isolate state per test execution context
    private static readonly AsyncLocal<bool> s_ShouldThrowException = new();
    private static readonly AsyncLocal<List<string>> s_ValidationResults = new();

    public static void ClearValidationResults()
    {
        s_ValidationResults.Value = [];
        s_ShouldThrowException.Value = false;
    }

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"TestSpecificValidationHandler<{typeof(TNotification).Name}>");

        if (ShouldThrowException)
        {
            throw new InvalidOperationException($"Validation failed for {typeof(TNotification).Name}");
        }

        ValidationResults.Add($"Validated {typeof(TNotification).Name} successfully");
        return default;
    }

    public static bool ShouldThrowException
    {
        get => s_ShouldThrowException.Value;
        set => s_ShouldThrowException.Value = value;
    }

    public static List<string> ValidationResults =>
        s_ValidationResults.Value ??= [];
}
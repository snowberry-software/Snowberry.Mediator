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
    // Use thread-safe collection for concurrent access
    public static ConcurrentBag<object> LoggedNotifications { get; } = [];

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericLoggingHandler<{typeof(TNotification).Name}>");
        LoggedNotifications.Add(notification);
        return ValueTask.CompletedTask;
    }

    public static void ClearLoggedNotifications()
    {
        // Clear by creating a new instance since ConcurrentBag doesn't have Clear()
        var currentBag = LoggedNotifications;
        while (currentBag.TryTake(out _))
        {
        }
    }
}

/// <summary>
/// Generic auditing handler that records all notifications for compliance
/// </summary>
public class GenericAuditingHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    // Use thread-safe collection for concurrent access
    public static ConcurrentDictionary<string, ConcurrentBag<object>> AuditLog { get; } = new();

    public async ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericAuditingHandler<{typeof(TNotification).Name}>");

        // Simulate async audit processing
        await Task.Delay(1, cancellationToken);

        string notificationType = typeof(TNotification).Name;
        var auditBag = AuditLog.GetOrAdd(notificationType, _ => []);
        auditBag.Add(notification);
    }

    public static void ClearAuditLog()
    {
        AuditLog.Clear();
    }
}

/// <summary>
/// Generic metrics handler that collects statistics on all notifications
/// </summary>
public class GenericMetricsHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    // Use thread-safe collections for concurrent access
    public static ConcurrentDictionary<string, int> NotificationCounts { get; } = new();
    public static ConcurrentDictionary<string, DateTime> LastProcessedTimes { get; } = new();

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericMetricsHandler<{typeof(TNotification).Name}>");

        string notificationType = typeof(TNotification).Name;

        // Thread-safe increment
        NotificationCounts.AddOrUpdate(notificationType, 1, (key, value) => value + 1);
        LastProcessedTimes[notificationType] = DateTime.UtcNow;

        return ValueTask.CompletedTask;
    }

    public static void ClearMetrics()
    {
        NotificationCounts.Clear();
        LastProcessedTimes.Clear();
    }
}

/// <summary>
/// Generic validation handler that could perform validation on any notification
/// </summary>
public class GenericValidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    // Use thread-safe collection for concurrent access
    public static ConcurrentBag<string> ValidationResults { get; } = [];
    public static bool ShouldThrowException { get; set; } = false;

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"GenericValidationHandler<{typeof(TNotification).Name}>");

        if (ShouldThrowException)
        {
            throw new InvalidOperationException($"Validation failed for {typeof(TNotification).Name}");
        }

        ValidationResults.Add($"Validated {typeof(TNotification).Name} successfully");
        return ValueTask.CompletedTask;
    }

    public static void ClearValidationResults()
    {
        // Clear by draining the bag
        while (ValidationResults.TryTake(out _))
        {
        }

        ShouldThrowException = false;
    }
}

/// <summary>
/// Test-specific validation handler that won't interfere with other tests
/// Uses AsyncLocal for test isolation to prevent race conditions
/// </summary>
public class TestSpecificValidationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    // Use AsyncLocal to isolate state per test execution context
    private static readonly AsyncLocal<bool> _shouldThrowException = new();
    private static readonly AsyncLocal<List<string>> _validationResults = new();

    public static bool ShouldThrowException
    {
        get => _shouldThrowException.Value;
        set => _shouldThrowException.Value = value;
    }

    public static List<string> ValidationResults =>
        _validationResults.Value ??= [];

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        NotificationHandlerExecutionTracker.RecordExecution($"TestSpecificValidationHandler<{typeof(TNotification).Name}>");

        if (ShouldThrowException)
        {
            throw new InvalidOperationException($"Validation failed for {typeof(TNotification).Name}");
        }

        ValidationResults.Add($"Validated {typeof(TNotification).Name} successfully");
        return ValueTask.CompletedTask;
    }

    public static void ClearValidationResults()
    {
        _validationResults.Value = [];
        _shouldThrowException.Value = false;
    }
}
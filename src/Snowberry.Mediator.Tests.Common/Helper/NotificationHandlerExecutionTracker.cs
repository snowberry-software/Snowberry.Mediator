using System.Collections.Concurrent;

namespace Snowberry.Mediator.Tests.Common.Helper;

/// <summary>
/// Tracks the execution of notification handlers for testing purposes.
/// Uses AsyncLocal to ensure test isolation when tests run in parallel.
/// </summary>
public static class NotificationHandlerExecutionTracker
{
    private static readonly AsyncLocal<ConcurrentBag<string>> _asyncLocalExecutions = new();

    private static ConcurrentBag<string> Executions =>
        _asyncLocalExecutions.Value ??= [];

    public static void RecordExecution(string handlerName)
    {
        Executions.Add(handlerName);
    }

    public static List<string> GetExecutions()
    {
        return Executions.ToList();
    }

    public static void Clear()
    {
        _asyncLocalExecutions.Value = [];
    }

    /// <summary>
    /// Initialize a new tracking context for the current async flow.
    /// </summary>
    public static void InitializeContext()
    {
        _asyncLocalExecutions.Value = [];
    }
}
using System.Collections.Concurrent;

namespace Snowberry.Mediator.Tests.Common.Helper;

/// <summary>
/// Tracks the execution of notification handlers for testing purposes.
/// Uses AsyncLocal to ensure test isolation when tests run in parallel.
/// </summary>
public static class NotificationHandlerExecutionTracker
{
    private static readonly AsyncLocal<ConcurrentBag<string>> s_AsyncLocalExecutions = new();

    public static void Clear()
    {
        s_AsyncLocalExecutions.Value = [];
    }

    public static List<string> GetExecutions()
    {
        return Executions.ToList();
    }

    /// <summary>
    /// Initialize a new tracking context for the current async flow.
    /// </summary>
    public static void InitializeContext()
    {
        s_AsyncLocalExecutions.Value = [];
    }

    public static void RecordExecution(string handlerName)
    {
        Executions.Add(handlerName);
    }

    private static ConcurrentBag<string> Executions =>
        s_AsyncLocalExecutions.Value ??= [];
}
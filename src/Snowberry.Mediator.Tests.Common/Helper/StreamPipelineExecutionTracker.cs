using System.Collections.Concurrent;

namespace Snowberry.Mediator.Tests.Common.Helper;

/// <summary>
/// Tracks the execution order of stream pipeline behaviors for testing purposes.
/// Uses AsyncLocal to ensure test isolation when tests run in parallel.
/// </summary>
public static class StreamPipelineExecutionTracker
{
    private static readonly AsyncLocal<ConcurrentQueue<string>> _asyncLocalExecutionOrder = new();

    private static ConcurrentQueue<string> ExecutionOrder =>
        _asyncLocalExecutionOrder.Value ??= new ConcurrentQueue<string>();

    public static void RecordExecution(string behaviorName)
    {
        ExecutionOrder.Enqueue(behaviorName);
    }

    public static List<string> GetExecutionOrder()
    {
        return ExecutionOrder.ToList();
    }

    public static void Clear()
    {
        var queue = ExecutionOrder;
        while (queue.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// Initialize a new tracking context for the current async flow.
    /// This is automatically called when needed, but can be called explicitly for clarity.
    /// </summary>
    public static void InitializeContext()
    {
        _asyncLocalExecutionOrder.Value = new ConcurrentQueue<string>();
    }
}
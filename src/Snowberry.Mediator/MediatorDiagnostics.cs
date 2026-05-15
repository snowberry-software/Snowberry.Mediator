using System.Diagnostics;

namespace Snowberry.Mediator;

/// <summary>
/// Provides the <see cref="ActivitySource"/> instances used for per-step mediator instrumentation
/// and process-wide opt-in flags that control whether per-step <see cref="Activity"/> objects are
/// created during dispatch.
/// </summary>
public static class MediatorDiagnostics
{
    /// <summary>
    /// The <see cref="ActivitySource"/> used when emitting an <see cref="Activity"/> for each
    /// notification handler invocation. The source name is <c>"Snowberry.Mediator.Notification"</c>.
    /// </summary>
    public static readonly ActivitySource s_NotificationSource = new("Snowberry.Mediator.Notification");

    /// <summary>
    /// The <see cref="ActivitySource"/> used when emitting an <see cref="Activity"/> for each
    /// pipeline behavior invocation. The source name is <c>"Snowberry.Mediator.Pipeline"</c>.
    /// </summary>
    public static readonly ActivitySource s_PipelineSource = new("Snowberry.Mediator.Pipeline");

    private static int s_NotificationEnabled;

    private static int s_PipelineEnabled;

    /// <summary>
    /// Enables per-notification-handler <see cref="Activity"/> creation for the current process.
    /// Once enabled the flag cannot be cleared. Calling this method when the flag is already set
    /// has no effect.
    /// </summary>
    public static void EnableNotificationSpans() => Volatile.Write(ref s_NotificationEnabled, 1);

    /// <summary>
    /// Enables per-pipeline-behavior <see cref="Activity"/> creation for the current process. Once
    /// enabled the flag cannot be cleared. Calling this method when the flag is already set has no
    /// effect.
    /// </summary>
    public static void EnablePipelineSpans() => Volatile.Write(ref s_PipelineEnabled, 1);

    /// <summary>
    /// Gets a value indicating whether per-notification-handler <see cref="Activity"/> creation is
    /// enabled for the current process. Returns <see langword="true"/> after
    /// <see cref="EnableNotificationSpans"/> has been called.
    /// </summary>
    public static bool IsNotificationEnabled => Volatile.Read(ref s_NotificationEnabled) != 0;

    /// <summary>
    /// Gets a value indicating whether per-pipeline-behavior <see cref="Activity"/> creation is
    /// enabled for the current process. Returns <see langword="true"/> after
    /// <see cref="EnablePipelineSpans"/> has been called.
    /// </summary>
    public static bool IsPipelineEnabled => Volatile.Read(ref s_PipelineEnabled) != 0;

    internal static void ResetForTests()
    {
        Volatile.Write(ref s_PipelineEnabled, 0);
        Volatile.Write(ref s_NotificationEnabled, 0);
    }
}
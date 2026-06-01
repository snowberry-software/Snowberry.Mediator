using System.Diagnostics;

namespace Snowberry.Mediator;

/// <summary>
/// Provides the <see cref="ActivitySource"/> instances used for per-step mediator instrumentation,
/// the names of the activities and tags those sources emit, and process-wide opt-in flags that
/// control whether per-step <see cref="Activity"/> objects are created during dispatch.
/// </summary>
public static class MediatorDiagnostics
{
    /// <summary>
    /// The instrumentation-scope version stamped onto the per-step <see cref="ActivitySource"/> instances, so
    /// emitted spans carry the library version (OpenTelemetry instrumentation-scope convention).
    /// </summary>
    public const string c_InstrumentationVersion = "1.1.0";

    /// <summary>The name of the <see cref="ActivitySource"/> used for per-pipeline-behavior spans.</summary>
    public const string c_PipelineSourceName = "Snowberry.Mediator.Pipeline";

    /// <summary>The name of the <see cref="ActivitySource"/> used for per-notification-handler spans.</summary>
    public const string c_NotificationSourceName = "Snowberry.Mediator.Notification";

    /// <summary>Prefix of the activity name emitted for each pipeline behavior invocation; appended with the behavior type name.</summary>
    public const string c_BehaviorActivityNamePrefix = "Mediator.Behavior ";

    /// <summary>Prefix of the activity name emitted for each notification handler invocation; appended with the handler type name.</summary>
    public const string c_HandlerActivityNamePrefix = "Mediator.Handler ";

    /// <summary>The request CLR type name tag applied to per-behavior activities.</summary>
    public const string c_RequestTypeTag = "snowberry.mediator.request.type";

    /// <summary>The notification CLR type name tag applied to per-handler activities.</summary>
    public const string c_NotificationTypeTag = "snowberry.mediator.notification.type";

    /// <summary>The pipeline behavior CLR type name tag applied to per-behavior activities.</summary>
    public const string c_BehaviorTypeTag = "snowberry.mediator.behavior.type";

    /// <summary>The notification handler CLR type name tag applied to per-handler activities.</summary>
    public const string c_HandlerTypeTag = "snowberry.mediator.handler.type";

    /// <summary>
    /// The <see cref="ActivitySource"/> used when emitting an <see cref="Activity"/> for each
    /// notification handler invocation. The source name is <see cref="c_NotificationSourceName"/>.
    /// </summary>
    public static readonly ActivitySource s_NotificationSource = new(c_NotificationSourceName, c_InstrumentationVersion);

    /// <summary>
    /// The <see cref="ActivitySource"/> used when emitting an <see cref="Activity"/> for each
    /// pipeline behavior invocation. The source name is <see cref="c_PipelineSourceName"/>.
    /// </summary>
    public static readonly ActivitySource s_PipelineSource = new(c_PipelineSourceName, c_InstrumentationVersion);

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

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Names and tag keys emitted by the Snowberry.Mediator OpenTelemetry instrumentation.
/// </summary>
public static class MediatorTelemetryConventions
{
    /// <summary>The default name of the dispatch <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>.</summary>
    public const string c_DefaultSourceName = "Snowberry.Mediator";

    /// <summary>The name of the <see cref="System.Diagnostics.ActivitySource"/> used for per-pipeline-behavior spans.</summary>
    public const string c_PipelineSourceName = MediatorDiagnostics.c_PipelineSourceName;

    /// <summary>The name of the <see cref="System.Diagnostics.ActivitySource"/> used for per-notification-handler spans.</summary>
    public const string c_NotificationSourceName = MediatorDiagnostics.c_NotificationSourceName;

    /// <summary>Names assigned to <see cref="System.Diagnostics.Activity"/> instances produced by the instrumentation.</summary>
    public static class ActivityNames
    {
        /// <summary>Prefix of the activity name emitted for each <c>SendAsync</c> dispatch; appended with the request type name.</summary>
        public const string c_SendPrefix = "Mediator.Send ";

        /// <summary>Prefix of the activity name emitted for each <c>CreateStreamAsync</c> dispatch; appended with the request type name.</summary>
        public const string c_StreamPrefix = "Mediator.Stream ";

        /// <summary>Prefix of the activity name emitted for each <c>PublishAsync</c> dispatch; appended with the notification type name.</summary>
        public const string c_PublishPrefix = "Mediator.Publish ";

        /// <summary>Prefix of the activity name emitted for each pipeline behavior invocation; appended with the behavior type name.</summary>
        public const string c_BehaviorPrefix = MediatorDiagnostics.c_BehaviorActivityNamePrefix;

        /// <summary>Prefix of the activity name emitted for each notification handler invocation; appended with the handler type name.</summary>
        public const string c_HandlerPrefix = MediatorDiagnostics.c_HandlerActivityNamePrefix;

        /// <summary>The name of the <see cref="System.Diagnostics.ActivityEvent"/> recorded when an enrichment callback throws.</summary>
        public const string c_EnrichmentFailedEvent = "snowberry.mediator.enrichment.failed";
    }

    /// <summary>Tag keys applied to <see cref="System.Diagnostics.Activity"/> instances and metric measurements.</summary>
    public static class Tags
    {
        /// <summary>The request CLR type name on a <c>SendAsync</c> / <c>CreateStreamAsync</c> activity.</summary>
        public const string c_RequestType = MediatorDiagnostics.c_RequestTypeTag;

        /// <summary>The response CLR type name on a <c>SendAsync</c> / <c>CreateStreamAsync</c> activity.</summary>
        public const string c_ResponseType = "snowberry.mediator.response.type";

        /// <summary>The notification CLR type name on a <c>PublishAsync</c> activity.</summary>
        public const string c_NotificationType = MediatorDiagnostics.c_NotificationTypeTag;

        /// <summary>The pipeline behavior CLR type name on a per-behavior activity.</summary>
        public const string c_BehaviorType = MediatorDiagnostics.c_BehaviorTypeTag;

        /// <summary>The notification handler CLR type name on a per-handler activity.</summary>
        public const string c_HandlerType = MediatorDiagnostics.c_HandlerTypeTag;

        /// <summary>The operation kind on a dispatch activity. See <see cref="Operations"/> for accepted values.</summary>
        public const string c_Operation = "snowberry.mediator.operation";

        /// <summary>The name of the enrichment hook that failed, on the <c>snowberry.mediator.enrichment.failed</c> activity event.</summary>
        public const string c_HookName = "snowberry.mediator.hook.name";

        /// <summary>The exception type, on the <c>snowberry.mediator.enrichment.failed</c> activity event.</summary>
        public const string c_ExceptionType = "exception.type";

        /// <summary>The exception message, on the <c>snowberry.mediator.enrichment.failed</c> activity event.</summary>
        public const string c_ExceptionMessage = "exception.message";

        /// <summary>The dispatched type name, applied to every metric measurement.</summary>
        public const string c_MetricType = "type";

        /// <summary>The dispatch outcome, applied to every metric measurement. See <see cref="Status"/> for accepted values.</summary>
        public const string c_MetricStatus = "status";
    }

    /// <summary>Values written to the <see cref="Tags.c_Operation"/> tag.</summary>
    public static class Operations
    {
        /// <summary>Identifies an activity created for a <c>SendAsync</c> dispatch.</summary>
        public const string c_Send = "send";

        /// <summary>Identifies an activity created for a <c>CreateStreamAsync</c> dispatch.</summary>
        public const string c_Stream = "stream";

        /// <summary>Identifies an activity created for a <c>PublishAsync</c> dispatch.</summary>
        public const string c_Publish = "publish";
    }

    /// <summary>Values written to the <see cref="Tags.c_MetricStatus"/> metric tag.</summary>
    public static class Status
    {
        /// <summary>The dispatch completed without throwing.</summary>
        public const string c_Success = "success";

        /// <summary>The dispatch threw, or for streams, did not reach a successful end of enumeration.</summary>
        public const string c_Failure = "failure";
    }

    /// <summary>Names of the metric instruments emitted by the dispatch decorator.</summary>
    public static class Instruments
    {
        /// <summary>Counter name for <c>SendAsync</c> dispatch count.</summary>
        public const string c_SendCount = "snowberry.mediator.send.count";

        /// <summary>Histogram name for <c>SendAsync</c> dispatch duration (milliseconds).</summary>
        public const string c_SendDuration = "snowberry.mediator.send.duration";

        /// <summary>Counter name for <c>CreateStreamAsync</c> dispatch count.</summary>
        public const string c_StreamCount = "snowberry.mediator.stream.count";

        /// <summary>Histogram name for <c>CreateStreamAsync</c> enumeration duration (milliseconds).</summary>
        public const string c_StreamDuration = "snowberry.mediator.stream.duration";

        /// <summary>Counter name for <c>PublishAsync</c> dispatch count.</summary>
        public const string c_PublishCount = "snowberry.mediator.publish.count";

        /// <summary>Histogram name for <c>PublishAsync</c> dispatch duration (milliseconds).</summary>
        public const string c_PublishDuration = "snowberry.mediator.publish.duration";

        /// <summary>Unit string applied to all duration histograms.</summary>
        public const string c_DurationUnit = "ms";
    }

    /// <summary>Identifiers used to tag enrichment-failure events with the source hook.</summary>
    public static class HookNames
    {
        /// <summary>Identifier of the <c>EnrichWithRequest</c> callback.</summary>
        public const string c_EnrichWithRequest = nameof(MediatorTelemetryOptions.EnrichWithRequest);

        /// <summary>Identifier of the <c>EnrichWithResponse</c> callback.</summary>
        public const string c_EnrichWithResponse = nameof(MediatorTelemetryOptions.EnrichWithResponse);

        /// <summary>Identifier of the <c>EnrichWithNotification</c> callback.</summary>
        public const string c_EnrichWithNotification = nameof(MediatorTelemetryOptions.EnrichWithNotification);

        /// <summary>Identifier of the <c>EnrichWithException</c> callback.</summary>
        public const string c_EnrichWithException = nameof(MediatorTelemetryOptions.EnrichWithException);
    }
}

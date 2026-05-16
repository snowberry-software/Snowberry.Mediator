using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

/// <summary>
/// Pins the literal values of <see cref="MediatorTelemetryConventions"/> and the corresponding
/// <see cref="MediatorDiagnostics"/> constants. The other OTel tests reference the constants
/// symbolically, so a typo in a constant's value (e.g. <c>"snowberry.mediatr.send.count"</c>) would
/// not break those tests. These assertions catch that.
/// </summary>
public class OpenTelemetry_ConventionPinTests
{
    [Fact]
    public void SourceNames_PinnedValues()
    {
        Assert.Equal("Snowberry.Mediator", MediatorTelemetryConventions.c_DefaultSourceName);
        Assert.Equal("Snowberry.Mediator.Pipeline", MediatorTelemetryConventions.c_PipelineSourceName);
        Assert.Equal("Snowberry.Mediator.Notification", MediatorTelemetryConventions.c_NotificationSourceName);
        Assert.Equal("Snowberry.Mediator.Pipeline", MediatorDiagnostics.c_PipelineSourceName);
        Assert.Equal("Snowberry.Mediator.Notification", MediatorDiagnostics.c_NotificationSourceName);
    }

    [Fact]
    public void ActivityNamePrefixes_PinnedValues()
    {
        Assert.Equal("Mediator.Send ", MediatorTelemetryConventions.ActivityNames.c_SendPrefix);
        Assert.Equal("Mediator.Stream ", MediatorTelemetryConventions.ActivityNames.c_StreamPrefix);
        Assert.Equal("Mediator.Publish ", MediatorTelemetryConventions.ActivityNames.c_PublishPrefix);
        Assert.Equal("Mediator.Behavior ", MediatorTelemetryConventions.ActivityNames.c_BehaviorPrefix);
        Assert.Equal("Mediator.Handler ", MediatorTelemetryConventions.ActivityNames.c_HandlerPrefix);
        Assert.Equal("snowberry.mediator.enrichment.failed", MediatorTelemetryConventions.ActivityNames.c_EnrichmentFailedEvent);
        Assert.Equal("Mediator.Behavior ", MediatorDiagnostics.c_BehaviorActivityNamePrefix);
        Assert.Equal("Mediator.Handler ", MediatorDiagnostics.c_HandlerActivityNamePrefix);
    }

    [Fact]
    public void Tags_PinnedValues()
    {
        Assert.Equal("snowberry.mediator.request.type", MediatorTelemetryConventions.Tags.c_RequestType);
        Assert.Equal("snowberry.mediator.response.type", MediatorTelemetryConventions.Tags.c_ResponseType);
        Assert.Equal("snowberry.mediator.notification.type", MediatorTelemetryConventions.Tags.c_NotificationType);
        Assert.Equal("snowberry.mediator.behavior.type", MediatorTelemetryConventions.Tags.c_BehaviorType);
        Assert.Equal("snowberry.mediator.handler.type", MediatorTelemetryConventions.Tags.c_HandlerType);
        Assert.Equal("snowberry.mediator.operation", MediatorTelemetryConventions.Tags.c_Operation);
        Assert.Equal("snowberry.mediator.hook.name", MediatorTelemetryConventions.Tags.c_HookName);
        Assert.Equal("exception.type", MediatorTelemetryConventions.Tags.c_ExceptionType);
        Assert.Equal("exception.message", MediatorTelemetryConventions.Tags.c_ExceptionMessage);
        Assert.Equal("type", MediatorTelemetryConventions.Tags.c_MetricType);
        Assert.Equal("status", MediatorTelemetryConventions.Tags.c_MetricStatus);

        Assert.Equal("snowberry.mediator.request.type", MediatorDiagnostics.c_RequestTypeTag);
        Assert.Equal("snowberry.mediator.notification.type", MediatorDiagnostics.c_NotificationTypeTag);
        Assert.Equal("snowberry.mediator.behavior.type", MediatorDiagnostics.c_BehaviorTypeTag);
        Assert.Equal("snowberry.mediator.handler.type", MediatorDiagnostics.c_HandlerTypeTag);
    }

    [Fact]
    public void TagValues_PinnedValues()
    {
        Assert.Equal("send", MediatorTelemetryConventions.Operations.c_Send);
        Assert.Equal("stream", MediatorTelemetryConventions.Operations.c_Stream);
        Assert.Equal("publish", MediatorTelemetryConventions.Operations.c_Publish);
        Assert.Equal("success", MediatorTelemetryConventions.Status.c_Success);
        Assert.Equal("failure", MediatorTelemetryConventions.Status.c_Failure);
    }

    [Fact]
    public void Instruments_PinnedValues()
    {
        Assert.Equal("snowberry.mediator.send.count", MediatorTelemetryConventions.Instruments.c_SendCount);
        Assert.Equal("snowberry.mediator.send.duration", MediatorTelemetryConventions.Instruments.c_SendDuration);
        Assert.Equal("snowberry.mediator.stream.count", MediatorTelemetryConventions.Instruments.c_StreamCount);
        Assert.Equal("snowberry.mediator.stream.duration", MediatorTelemetryConventions.Instruments.c_StreamDuration);
        Assert.Equal("snowberry.mediator.publish.count", MediatorTelemetryConventions.Instruments.c_PublishCount);
        Assert.Equal("snowberry.mediator.publish.duration", MediatorTelemetryConventions.Instruments.c_PublishDuration);
        Assert.Equal("ms", MediatorTelemetryConventions.Instruments.c_DurationUnit);
    }

    [Fact]
    public void HookNames_PinnedValues()
    {
        Assert.Equal(nameof(MediatorTelemetryOptions.EnrichWithRequest), MediatorTelemetryConventions.HookNames.c_EnrichWithRequest);
        Assert.Equal(nameof(MediatorTelemetryOptions.EnrichWithResponse), MediatorTelemetryConventions.HookNames.c_EnrichWithResponse);
        Assert.Equal(nameof(MediatorTelemetryOptions.EnrichWithNotification), MediatorTelemetryConventions.HookNames.c_EnrichWithNotification);
        Assert.Equal(nameof(MediatorTelemetryOptions.EnrichWithException), MediatorTelemetryConventions.HookNames.c_EnrichWithException);
    }
}

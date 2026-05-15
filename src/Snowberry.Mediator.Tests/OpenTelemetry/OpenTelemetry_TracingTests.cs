using Snowberry.Mediator.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_TracingTests
{
    [Fact]
    public async Task CreateStreamAsync_ProducesActivity_SpanningEnumeration()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(NumberStreamRequestHandler)];
        });

        // Snapshot activities count mid-enumeration: the stream activity must still be live
        // (un-stopped) while items are being yielded.
        int activitiesBeforeFinish = -1;
        int count = 0;
        await foreach (var _ in fx.Mediator.CreateStreamAsync(new NumberStreamRequest { Count = 5 }))
        {
            if (count == 0) activitiesBeforeFinish = fx.StoppedActivities.Count;
            count++;
        }

        Assert.Equal(5, count);
        Assert.Equal(0, activitiesBeforeFinish); // not stopped during enumeration
        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(MediatorTelemetryConventions.ActivityNames.c_StreamPrefix + nameof(NumberStreamRequest), activity.OperationName);
        Assert.Equal(MediatorTelemetryConventions.Operations.c_Stream, activity.GetTagItem(MediatorTelemetryConventions.Tags.c_Operation));
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Ok, activity.Status);
    }

    [Fact]
    public async Task EnableTracing_False_DoesNotProduceActivities_ButMetricsStillEmit()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.EnableTracing = false);

        await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Empty(fx.StoppedActivities);
        Assert.NotEmpty(fx.Measurements);
    }

    [Fact]
    public async Task NestedSendAsync_ChildActivityParentedToOuter()
    {
        // Outer handler that calls SendAsync again from inside.
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(NestingRequestHandler), typeof(CounterRequestHandler)];
        });

        await fx.Mediator.SendAsync(new NestingRequest());

        Assert.Equal(2, fx.StoppedActivities.Count);
        var inner = fx.StoppedActivities.Single(a => a.OperationName == MediatorTelemetryConventions.ActivityNames.c_SendPrefix + nameof(CounterRequest));
        var outer = fx.StoppedActivities.Single(a => a.OperationName == MediatorTelemetryConventions.ActivityNames.c_SendPrefix + nameof(NestingRequest));
        Assert.Equal(outer.Id, inner.ParentId);
        Assert.Equal(outer.TraceId, inner.TraceId);
    }

    [Fact]
    public async Task PublishAsync_ProducesActivity_ForAllHandlers()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        });

        await fx.Mediator.PublishAsync(new SimpleNotification { Message = "hi" });

        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(MediatorTelemetryConventions.ActivityNames.c_PublishPrefix + nameof(SimpleNotification), activity.OperationName);
        Assert.Equal(nameof(SimpleNotification), activity.GetTagItem(MediatorTelemetryConventions.Tags.c_NotificationType));
        Assert.Equal(MediatorTelemetryConventions.Operations.c_Publish, activity.GetTagItem(MediatorTelemetryConventions.Tags.c_Operation));
    }

    [Fact]
    public async Task SendAsync_HandlerThrows_ActivityStatusIsError()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(ThrowingRequestHandler)];
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fx.Mediator.SendAsync(new ThrowingRequest()));

        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, activity.Status);
        Assert.Equal("boom", activity.StatusDescription);
    }

    [Fact]
    public async Task SendAsync_ProducesActivity_WithCorrectNameAndTags()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
        });

        var result = await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Equal(5, result);
        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(MediatorTelemetryConventions.ActivityNames.c_SendPrefix + nameof(CounterRequest), activity.OperationName);
        Assert.Equal(System.Diagnostics.ActivityKind.Internal, activity.Kind);
        Assert.Equal(nameof(CounterRequest), activity.GetTagItem(MediatorTelemetryConventions.Tags.c_RequestType));
        Assert.Equal(nameof(Int32), activity.GetTagItem(MediatorTelemetryConventions.Tags.c_ResponseType));
        Assert.Equal(MediatorTelemetryConventions.Operations.c_Send, activity.GetTagItem(MediatorTelemetryConventions.Tags.c_Operation));
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Ok, activity.Status);
    }
}

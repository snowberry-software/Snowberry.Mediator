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
        Assert.Equal("Mediator.Stream NumberStreamRequest", activity.OperationName);
        Assert.Equal("stream", activity.GetTagItem("snowberry.mediator.operation"));
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
        var inner = fx.StoppedActivities.Single(a => a.OperationName == "Mediator.Send CounterRequest");
        var outer = fx.StoppedActivities.Single(a => a.OperationName == "Mediator.Send NestingRequest");
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
        Assert.Equal("Mediator.Publish SimpleNotification", activity.OperationName);
        Assert.Equal("SimpleNotification", activity.GetTagItem("snowberry.mediator.notification.type"));
        Assert.Equal("publish", activity.GetTagItem("snowberry.mediator.operation"));
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
        Assert.Equal("Mediator.Send CounterRequest", activity.OperationName);
        Assert.Equal(System.Diagnostics.ActivityKind.Internal, activity.Kind);
        Assert.Equal("CounterRequest", activity.GetTagItem("snowberry.mediator.request.type"));
        Assert.Equal("Int32", activity.GetTagItem("snowberry.mediator.response.type"));
        Assert.Equal("send", activity.GetTagItem("snowberry.mediator.operation"));
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Ok, activity.Status);
    }
}
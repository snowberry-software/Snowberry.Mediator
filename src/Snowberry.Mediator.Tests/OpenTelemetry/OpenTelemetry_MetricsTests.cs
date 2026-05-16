using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_MetricsTests
{
    [Fact]
    public async Task CreateStreamAsync_RecordsStreamCounter_WithSuccessOnFullEnumeration()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(NumberStreamRequestHandler)];
        });

        int sum = 0;
        await foreach (var i in fx.Mediator.CreateStreamAsync(new NumberStreamRequest { Count = 3 }))
            sum += i;

        Assert.Equal(6, sum);
        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_StreamCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Success, count.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task EnableMetrics_False_NoMeasurementsButTracingStillEmits()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.EnableMetrics = false);

        await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Empty(fx.Measurements);
        Assert.NotEmpty(fx.StoppedActivities);
    }

    [Fact]
    public async Task PublishAsync_RecordsPublishCounter()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        });

        await fx.Mediator.PublishAsync(new SimpleNotification());

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_PublishCount);
        Assert.Equal(nameof(SimpleNotification), count.Tag(MediatorTelemetryConventions.Tags.c_MetricType));
        Assert.Equal(MediatorTelemetryConventions.Status.c_Success, count.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task SendAsync_HandlerThrows_RecordsFailureStatus()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(ThrowingRequestHandler)];
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fx.Mediator.SendAsync(new ThrowingRequest()));

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_SendCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Failure, count.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task PublishAsync_NoHandlersRegistered_SurfacesExceptionAndRecordsFailure()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            // A request handler is required for the registration extension to do anything, but
            // we deliberately register no notification handlers so the mediator has no notification
            // registry to dispatch through.
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
        });

        await Assert.ThrowsAsync<NotificationHandlerNotFoundException>(async () =>
            await fx.Mediator.PublishAsync(new SimpleNotification()));

        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, activity.Status);
        Assert.Equal(MediatorTelemetryConventions.ActivityNames.c_PublishPrefix + nameof(SimpleNotification), activity.OperationName);

        var countMeasurement = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_PublishCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Failure, countMeasurement.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task SendAsync_Success_RecordsCountAndDurationWithSuccessStatus()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
        });

        await fx.Mediator.SendAsync(new CounterRequest());

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_SendCount);
        Assert.Equal(1d, count.Value);
        Assert.Equal(nameof(CounterRequest), count.Tag(MediatorTelemetryConventions.Tags.c_MetricType));
        Assert.Equal(MediatorTelemetryConventions.Status.c_Success, count.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));

        var duration = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_SendDuration);
        Assert.InRange(duration.Value, 0d, 60_000d);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Success, duration.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }
}

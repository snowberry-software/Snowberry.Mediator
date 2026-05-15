using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_MetricsTests
{
    [Fact]
    public async Task SendAsync_Success_RecordsCountAndDurationWithSuccessStatus()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
        });

        await fx.Mediator.SendAsync(new CounterRequest());

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == "snowberry.mediator.send.count");
        Assert.Equal(1d, count.Value);
        Assert.Equal("CounterRequest", count.Tag("type"));
        Assert.Equal("success", count.Tag("status"));

        var duration = Assert.Single(fx.Measurements, m => m.InstrumentName == "snowberry.mediator.send.duration");
        Assert.InRange(duration.Value, 0d, 60_000d);
        Assert.Equal("success", duration.Tag("status"));
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

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == "snowberry.mediator.send.count");
        Assert.Equal("failure", count.Tag("status"));
    }

    [Fact]
    public async Task PublishAsync_RecordsPublishCounter()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        });

        await fx.Mediator.PublishAsync(new SimpleNotification());

        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == "snowberry.mediator.publish.count");
        Assert.Equal("SimpleNotification", count.Tag("type"));
        Assert.Equal("success", count.Tag("status"));
    }

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
        var count = Assert.Single(fx.Measurements, m => m.InstrumentName == "snowberry.mediator.stream.count");
        Assert.Equal("success", count.Tag("status"));
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
}

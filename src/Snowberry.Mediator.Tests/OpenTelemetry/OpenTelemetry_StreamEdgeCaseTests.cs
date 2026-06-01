using Snowberry.Mediator.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_StreamEdgeCaseTests
{
    [Fact]
    public async Task Stream_CancelledMidEnumeration_ActivityStoppedWithFailure()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(NumberStreamRequestHandler)];
        });

        using var cts = new CancellationTokenSource();
        int observed = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in fx.Mediator.CreateStreamAsync(new NumberStreamRequest { Count = 10 }, cts.Token))
            {
                observed++;
                if (observed == 2)
                    cts.Cancel();
            }
        });

        Assert.True(observed >= 2);
        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, activity.Status);

        var statusMeasurement = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_StreamCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Failure, statusMeasurement.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task Stream_MoveNextThrows_ActivityStoppedWithFailure_ExceptionPropagates()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(ThrowingStreamRequestHandler)];
        });

        int observed = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in fx.Mediator.CreateStreamAsync(new ThrowingStreamRequest()))
                observed++;
        });

        Assert.Equal(1, observed); // handler yields 1 before throwing
        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, activity.Status);

        var countMeasurement = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_StreamCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Failure, countMeasurement.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }

    [Fact]
    public async Task Stream_ConsumerBreaksEarly_ActivityDisposed_WithFailureStatus()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(NumberStreamRequestHandler)];
        });

        int observed = 0;
        await foreach (var _ in fx.Mediator.CreateStreamAsync(new NumberStreamRequest { Count = 10 }))
        {
            observed++;
            if (observed == 2)
                break;
        }

        Assert.Equal(2, observed);
        var activity = Assert.Single(fx.StoppedActivities);
        // Activity was disposed (it's in StoppedActivities) but status reflects incomplete enumeration.
        Assert.Equal(System.Diagnostics.ActivityStatusCode.Error, activity.Status);

        var countMeasurement = Assert.Single(fx.Measurements, m => m.InstrumentName == MediatorTelemetryConventions.Instruments.c_StreamCount);
        Assert.Equal(MediatorTelemetryConventions.Status.c_Failure, countMeasurement.Tag(MediatorTelemetryConventions.Tags.c_MetricStatus));
    }
}

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_BuilderExtensionTests
{
    [Fact]
    public void TracerProvider_AddSnowberryMediatorInstrumentation_RegistersAllThreeSources()
    {
        var captured = new List<System.Diagnostics.Activity>();
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSnowberryMediatorInstrumentation()
            .AddInMemoryExporter(captured)
            .Build();

        using var topLevel = new System.Diagnostics.ActivitySource("Snowberry.Mediator");
        using var pipeline = new System.Diagnostics.ActivitySource("Snowberry.Mediator.Pipeline");
        using var notif = new System.Diagnostics.ActivitySource("Snowberry.Mediator.Notification");

        topLevel.StartActivity("a")?.Dispose();
        pipeline.StartActivity("b")?.Dispose();
        notif.StartActivity("c")?.Dispose();

        provider!.ForceFlush();

        Assert.Equal(3, captured.Count);
    }

    [Fact]
    public void TracerProvider_NamedOverload_UsesCustomTopLevelName_KeepsPipelineAndNotificationFixed()
    {
        var captured = new List<System.Diagnostics.Activity>();
        using var provider = Sdk.CreateTracerProviderBuilder()
            .AddSnowberryMediatorInstrumentation("Custom.Source")
            .AddInMemoryExporter(captured)
            .Build();

        using var custom = new System.Diagnostics.ActivitySource("Custom.Source");
        using var notUsed = new System.Diagnostics.ActivitySource("Snowberry.Mediator"); // not registered

        custom.StartActivity("yes")?.Dispose();
        notUsed.StartActivity("no")?.Dispose();

        provider!.ForceFlush();
        Assert.Single(captured);
        Assert.Equal("yes", captured[0].OperationName);
    }

    [Fact]
    public void MeterProvider_AddSnowberryMediatorInstrumentation_RegistersDefaultMeter()
    {
        var captured = new List<Metric>();
        using var provider = Sdk.CreateMeterProviderBuilder()
            .AddSnowberryMediatorInstrumentation()
            .AddInMemoryExporter(captured)
            .Build();

        using var meter = new System.Diagnostics.Metrics.Meter("Snowberry.Mediator");
        var counter = meter.CreateCounter<long>("test.counter");
        counter.Add(1);

        provider!.ForceFlush();
        Assert.NotEmpty(captured);
    }
}

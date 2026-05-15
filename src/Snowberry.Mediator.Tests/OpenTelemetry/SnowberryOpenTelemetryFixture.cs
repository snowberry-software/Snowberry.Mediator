using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

internal sealed class SnowberryOpenTelemetryFixture : IDisposable
{
    private readonly ServiceContainer _container;
    private readonly OpenTelemetryListenerCapture _capture;
    public string SourceName { get; }

    public SnowberryOpenTelemetryFixture(
        Action<MediatorOptions> configureMediator,
        Action<global::Snowberry.Mediator.OpenTelemetry.MediatorTelemetryOptions>? configureTelemetry = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        global::Snowberry.Mediator.MediatorDiagnostics.ResetForTests();

        var capturedOptions = new global::Snowberry.Mediator.OpenTelemetry.MediatorTelemetryOptions();
        configureTelemetry?.Invoke(capturedOptions);
        SourceName = capturedOptions.SourceName;
        _capture = new OpenTelemetryListenerCapture(SourceName);

        _container = new ServiceContainer();
        // OTel must be registered BEFORE AddSnowberryMediator on Snowberry-DI.
        _container.AddSnowberryMediatorOpenTelemetry(o =>
        {
            o.SourceName = capturedOptions.SourceName;
            o.EnableTracing = capturedOptions.EnableTracing;
            o.EnableMetrics = capturedOptions.EnableMetrics;
            o.EnablePipelineBehaviorSpans = capturedOptions.EnablePipelineBehaviorSpans;
            o.EnableNotificationHandlerSpans = capturedOptions.EnableNotificationHandlerSpans;
            o.EnrichWithRequest = capturedOptions.EnrichWithRequest;
            o.EnrichWithResponse = capturedOptions.EnrichWithResponse;
            o.EnrichWithNotification = capturedOptions.EnrichWithNotification;
            o.EnrichWithException = capturedOptions.EnrichWithException;
            o.Filter = capturedOptions.Filter;
        }, lifetime: lifetime);
        _container.AddSnowberryMediator(configureMediator, lifetime);

        Mediator = _container.GetRequiredService<IMediator>();
    }

    public IMediator Mediator { get; }

    public ServiceContainer Container => _container;

    public IReadOnlyList<System.Diagnostics.Activity> StoppedActivities => _capture.StoppedActivities;

    public IReadOnlyList<MetricMeasurement> Measurements => _capture.Measurements;

    public void Dispose()
    {
        _capture.Dispose();
        _container.Dispose();
        global::Snowberry.Mediator.MediatorDiagnostics.ResetForTests();
    }
}

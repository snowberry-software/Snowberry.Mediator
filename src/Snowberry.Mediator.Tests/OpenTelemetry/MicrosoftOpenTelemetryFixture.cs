using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

internal sealed class MicrosoftOpenTelemetryFixture : IDisposable
{
    private readonly OpenTelemetryListenerCapture _capture;
    private readonly ServiceProvider _provider;

    public MicrosoftOpenTelemetryFixture(
        Action<MediatorOptions> configureMediator,
        Action<MediatorTelemetryOptions>? configureTelemetry = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        MediatorDiagnostics.ResetForTests();

        var capturedOptions = new MediatorTelemetryOptions();
        configureTelemetry?.Invoke(capturedOptions);
        SourceName = capturedOptions.SourceName;
        _capture = new OpenTelemetryListenerCapture(SourceName);

        var services = new ServiceCollection();
        services.AddSnowberryMediator(configureMediator, lifetime);
        services.AddSnowberryMediatorOpenTelemetry(o =>
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
        });

        _provider = services.BuildServiceProvider();
        Mediator = _provider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _capture.Dispose();
        _provider.Dispose();
        MediatorDiagnostics.ResetForTests();
    }

    public IReadOnlyList<MetricMeasurement> Measurements => _capture.Measurements;

    public IMediator Mediator { get; }

    public IServiceProvider Services => _provider;
    public string SourceName { get; }

    public IReadOnlyList<System.Diagnostics.Activity> StoppedActivities => _capture.StoppedActivities;
}
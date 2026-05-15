using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;

namespace Snowberry.Mediator.Benchmarks;

/// <summary>
/// Per-step span benchmarks. Lives in a separate class because <see cref="MediatorDiagnostics"/>
/// uses process-wide static flags — enabling them in <c>[GlobalSetup]</c> would pollute the
/// regression-guard benchmarks in <see cref="MediatorBenchmarks"/>. BDN runs each benchmark class
/// as a separate process when invoked via <c>--filter</c>, so they stay isolated.
/// </summary>
[MemoryDiagnoser]
public class MediatorPerStepSpanBenchmarks
{
    private readonly MixedNotification3 _mixedNotification3 = new();
    private readonly Specific10Request _specific10Request = new();
    private IMediator _mediatorPublishMixed3WithSpans = null!;

    private IMediator _mediatorSpecific10WithSpans = null!;

    private static IMediator BuildMediatorWithOtel(Action<MediatorOptions> configure, Action<OpenTelemetry.MediatorTelemetryOptions> configureTelemetry)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(configure, ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry(configureTelemetry);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }

    /// <summary>
    /// Per-handler-span overhead with the flag enabled but no listener attached.
    /// </summary>
    [Benchmark]
    public ValueTask Publish_Mixed3_PerHandlerSpans_NoListener()
        => _mediatorPublishMixed3WithSpans.PublishAsync(_mixedNotification3);

    /// <summary>
    /// Per-behavior-span overhead with the flag enabled but no listener attached. Measures the cost
    /// of the slow-path async state machine being instantiated when <c>StartActivity</c> still
    /// returns null. Expected: ~50–80 ns / ~80 B per behavior step.
    /// </summary>
    [Benchmark]
    public ValueTask<int> Send_Specific10_PerBehaviorSpans_NoListener()
        => _mediatorSpecific10WithSpans.SendAsync(_specific10Request);

    [GlobalSetup]
    public void Setup()
    {
        MediatorDiagnostics.ResetForTests();

        _mediatorSpecific10WithSpans = BuildMediatorWithOtel(
            opt =>
            {
                opt.RequestHandlerTypes = [typeof(Specific10RequestHandler)];
                opt.PipelineBehaviorTypes =
                [
                    typeof(Specific10Behavior1),
                    typeof(Specific10Behavior2),
                    typeof(Specific10Behavior3),
                    typeof(Specific10Behavior4),
                    typeof(Specific10Behavior5),
                    typeof(Specific10Behavior6),
                    typeof(Specific10Behavior7),
                    typeof(Specific10Behavior8),
                    typeof(Specific10Behavior9),
                    typeof(Specific10Behavior10),
                ];
            },
            tel => tel.EnablePipelineBehaviorSpans = true);

        _mediatorPublishMixed3WithSpans = BuildMediatorWithOtel(
            opt =>
            {
                opt.NotificationHandlerTypes =
                [
                    typeof(MixedNotification3Specific1),
                    typeof(MixedNotification3Specific2),
                    typeof(MixedOpenNotificationHandler<>),
                ];
            },
            tel => tel.EnableNotificationHandlerSpans = true);
    }
}
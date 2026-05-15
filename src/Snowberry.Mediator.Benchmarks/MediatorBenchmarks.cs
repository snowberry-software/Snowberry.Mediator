using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;

namespace Snowberry.Mediator.Benchmarks;

[MemoryDiagnoser]
public class MediatorBenchmarks
{
    private readonly Mixed4Request _mixed4Request = new();
    private readonly MixedNotification3 _mixedNotification3 = new();

    private readonly NoPipelineRequest _noPipelineRequest = new();
    private readonly OpenGeneric1AsyncRequest _openGeneric1AsyncRequest = new();
    private readonly OpenGeneric1Request _openGeneric1Request = new();
    private readonly OpenGeneric3Request _openGeneric3Request = new();
    private readonly OpenGenericNotification3 _openGenericNotification3 = new();
    private readonly Specific10Request _specific10Request = new();
    private readonly Specific1AsyncRequest _specific1AsyncRequest = new();
    private readonly Specific1Request _specific1Request = new();
    private readonly Specific3Request _specific3Request = new();
    private readonly SpecificNotification3 _specificNotification3 = new();
    private readonly StreamNoPipelineRequest _streamNoPipelineRequest = new();
    private readonly StreamOpenGeneric3Request _streamOpenGeneric3Request = new();
    private readonly StreamSpecific10Request _streamSpecific10Request = new();
    private readonly StreamSpecific1Request _streamSpecific1Request = new();
    private readonly StreamSpecific3Request _streamSpecific3Request = new();
    private IMediator _mediatorMixed4 = null!;
    private IMediator _mediatorNoPipeline = null!;
    private IMediator _mediatorOpenGeneric1 = null!;
    private IMediator _mediatorOpenGeneric1Async = null!;
    private IMediator _mediatorOpenGeneric3 = null!;
    private IMediator _mediatorPublishMixed3 = null!;
    private IMediator _mediatorPublishOpenGeneric3 = null!;
    private IMediator _mediatorPublishSpecific3 = null!;
    private IMediator _mediatorSpecific1 = null!;
    private IMediator _mediatorSpecific10 = null!;
    private IMediator _mediatorSpecific1Async = null!;
    private IMediator _mediatorSpecific3 = null!;
    private IMediator _mediatorStreamNoPipeline = null!;
    private IMediator _mediatorStreamOpenGeneric3 = null!;
    private IMediator _mediatorStreamSpecific1 = null!;
    private IMediator _mediatorStreamSpecific10 = null!;
    private IMediator _mediatorStreamSpecific3 = null!;

    // OTel-decorated mediators (no listener attached → fast path).
    private IMediator _mediatorNoPipelineOtel = null!;
    private IMediator _mediatorStreamNoPipelineOtel = null!;
    private IMediator _mediatorPublishSpecific3Otel = null!;

    private static IMediator BuildMediator(Action<MediatorOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(configure, ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }

    private static IMediator BuildMediatorWithOtel(Action<MediatorOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(configure, ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }

    [Benchmark]
    public ValueTask Publish_Mixed3()
        => _mediatorPublishMixed3.PublishAsync(_mixedNotification3);

    [Benchmark]
    public ValueTask Publish_OpenGeneric3()
        => _mediatorPublishOpenGeneric3.PublishAsync(_openGenericNotification3);

    // ---------- Publish ----------

    [Benchmark]
    public ValueTask Publish_Specific3()
        => _mediatorPublishSpecific3.PublishAsync(_specificNotification3);

    [Benchmark]
    public ValueTask<int> Send_Mixed4()
        => _mediatorMixed4.SendAsync(_mixed4Request);

    // ---------- Send ----------

    [Benchmark(Baseline = true)]
    public ValueTask<int> Send_NoPipeline()
        => _mediatorNoPipeline.SendAsync(_noPipelineRequest);

    [Benchmark]
    public ValueTask<int> Send_OpenGeneric1()
        => _mediatorOpenGeneric1.SendAsync(_openGeneric1Request);

    [Benchmark]
    public ValueTask<int> Send_OpenGeneric1_Async()
        => _mediatorOpenGeneric1Async.SendAsync(_openGeneric1AsyncRequest);

    [Benchmark]
    public ValueTask<int> Send_OpenGeneric3()
        => _mediatorOpenGeneric3.SendAsync(_openGeneric3Request);

    [Benchmark]
    public ValueTask<int> Send_Specific1()
        => _mediatorSpecific1.SendAsync(_specific1Request);

    [Benchmark]
    public ValueTask<int> Send_Specific10()
        => _mediatorSpecific10.SendAsync(_specific10Request);

    // ---------- Phase 0.5 diagnostic variants (async/await - exposes AsyncStateMachineBox vs delegate) ----------

    [Benchmark]
    public ValueTask<int> Send_Specific1_Async()
        => _mediatorSpecific1Async.SendAsync(_specific1AsyncRequest);

    [Benchmark]
    public ValueTask<int> Send_Specific3()
        => _mediatorSpecific3.SendAsync(_specific3Request);

    [GlobalSetup]
    public void Setup()
    {
        _mediatorNoPipeline = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(NoPipelineRequestHandler)];
        });

        _mediatorSpecific1 = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(Specific1RequestHandler)];
            opt.PipelineBehaviorTypes = [typeof(Specific1Behavior1)];
        });

        _mediatorSpecific3 = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(Specific3RequestHandler)];
            opt.PipelineBehaviorTypes =
            [
                typeof(Specific3Behavior1),
                typeof(Specific3Behavior2),
                typeof(Specific3Behavior3),
            ];
        });

        _mediatorSpecific10 = BuildMediator(opt =>
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
        });

        _mediatorOpenGeneric1 = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(OpenGeneric1RequestHandler)];
            opt.PipelineBehaviorTypes = [typeof(OpenBehavior1<,>)];
        });

        _mediatorOpenGeneric3 = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(OpenGeneric3RequestHandler)];
            opt.PipelineBehaviorTypes =
            [
                typeof(OpenBehavior1<,>),
                typeof(OpenBehavior2<,>),
                typeof(OpenBehavior3<,>),
            ];
        });

        _mediatorMixed4 = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(Mixed4RequestHandler)];
            opt.PipelineBehaviorTypes =
            [
                typeof(Mixed4SpecificBehavior1),
                typeof(Mixed4SpecificBehavior2),
                typeof(MixedOpenBehavior1<,>),
                typeof(MixedOpenBehavior2<,>),
            ];
        });

        _mediatorPublishSpecific3 = BuildMediator(opt =>
        {
            opt.NotificationHandlerTypes =
            [
                typeof(SpecificNotification3Handler1),
                typeof(SpecificNotification3Handler2),
                typeof(SpecificNotification3Handler3),
            ];
        });

        _mediatorPublishOpenGeneric3 = BuildMediator(opt =>
        {
            opt.NotificationHandlerTypes =
            [
                typeof(OpenNotificationHandler1<>),
                typeof(OpenNotificationHandler2<>),
                typeof(OpenNotificationHandler3<>),
            ];
            // We need at least one concrete notification "registered" implicitly via the publish call;
            // open-generic handlers are constructed on demand.
        });

        _mediatorPublishMixed3 = BuildMediator(opt =>
        {
            opt.NotificationHandlerTypes =
            [
                typeof(MixedNotification3Specific1),
                typeof(MixedNotification3Specific2),
                typeof(MixedOpenNotificationHandler<>),
            ];
        });

        _mediatorStreamNoPipeline = BuildMediator(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamNoPipelineRequestHandler)];
        });

        _mediatorStreamSpecific1 = BuildMediator(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamSpecific1RequestHandler)];
            opt.StreamPipelineBehaviorTypes = [typeof(StreamSpecific1Behavior1)];
        });

        _mediatorStreamSpecific3 = BuildMediator(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamSpecific3RequestHandler)];
            opt.StreamPipelineBehaviorTypes =
            [
                typeof(StreamSpecific3Behavior1),
                typeof(StreamSpecific3Behavior2),
                typeof(StreamSpecific3Behavior3),
            ];
        });

        _mediatorStreamSpecific10 = BuildMediator(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamSpecific10RequestHandler)];
            opt.StreamPipelineBehaviorTypes =
            [
                typeof(StreamSpecific10Behavior1),
                typeof(StreamSpecific10Behavior2),
                typeof(StreamSpecific10Behavior3),
                typeof(StreamSpecific10Behavior4),
                typeof(StreamSpecific10Behavior5),
                typeof(StreamSpecific10Behavior6),
                typeof(StreamSpecific10Behavior7),
                typeof(StreamSpecific10Behavior8),
                typeof(StreamSpecific10Behavior9),
                typeof(StreamSpecific10Behavior10),
            ];
        });

        _mediatorStreamOpenGeneric3 = BuildMediator(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamOpenGeneric3RequestHandler)];
            opt.StreamPipelineBehaviorTypes =
            [
                typeof(OpenStreamBehavior1<,>),
                typeof(OpenStreamBehavior2<,>),
                typeof(OpenStreamBehavior3<,>),
            ];
        });

        _mediatorSpecific1Async = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(Specific1AsyncRequestHandler)];
            opt.PipelineBehaviorTypes = [typeof(Specific1AsyncBehavior1)];
        });

        _mediatorOpenGeneric1Async = BuildMediator(opt =>
        {
            opt.RequestHandlerTypes = [typeof(OpenGeneric1AsyncRequestHandler)];
            opt.PipelineBehaviorTypes = [typeof(OpenAsyncBehavior1<,>)];
        });

        // OTel-decorated mediators with no listener attached. These measure the dispatch decorator's
        // fast-path overhead vs the un-decorated baselines (`Send_NoPipeline`, `Stream_NoPipeline_Enumerate10`,
        // `Publish_Specific3`). Expected: +1–3 ns / 0 B.
        _mediatorNoPipelineOtel = BuildMediatorWithOtel(opt =>
        {
            opt.RequestHandlerTypes = [typeof(NoPipelineRequestHandler)];
        });

        _mediatorStreamNoPipelineOtel = BuildMediatorWithOtel(opt =>
        {
            opt.StreamRequestHandlerTypes = [typeof(StreamNoPipelineRequestHandler)];
        });

        _mediatorPublishSpecific3Otel = BuildMediatorWithOtel(opt =>
        {
            opt.NotificationHandlerTypes =
            [
                typeof(SpecificNotification3Handler1),
                typeof(SpecificNotification3Handler2),
                typeof(SpecificNotification3Handler3),
            ];
        });
    }

    // ---------- OTel decorator (no listener) — fast-path regression guard ----------

    [Benchmark]
    public ValueTask<int> Send_NoPipeline_OtelDecoratedNoListener()
        => _mediatorNoPipelineOtel.SendAsync(_noPipelineRequest);

    [Benchmark]
    public async ValueTask<int> Stream_NoPipeline_OtelDecoratedNoListener_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamNoPipelineOtel.CreateStreamAsync(_streamNoPipelineRequest))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public ValueTask Publish_Specific3_OtelDecoratedNoListener()
        => _mediatorPublishSpecific3Otel.PublishAsync(_specificNotification3);

    // ---------- Stream ----------

    [Benchmark]
    public async ValueTask<int> Stream_NoPipeline_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamNoPipeline.CreateStreamAsync(_streamNoPipelineRequest))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public async ValueTask<int> Stream_OpenGeneric3_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamOpenGeneric3.CreateStreamAsync(_streamOpenGeneric3Request))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public async ValueTask<int> Stream_Specific10_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamSpecific10.CreateStreamAsync(_streamSpecific10Request))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public async ValueTask<int> Stream_Specific1_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamSpecific1.CreateStreamAsync(_streamSpecific1Request))
        {
            sum += i;
        }
        return sum;
    }

    [Benchmark]
    public async ValueTask<int> Stream_Specific3_Enumerate10()
    {
        int sum = 0;
        await foreach (var i in _mediatorStreamSpecific3.CreateStreamAsync(_streamSpecific3Request))
        {
            sum += i;
        }
        return sum;
    }
}
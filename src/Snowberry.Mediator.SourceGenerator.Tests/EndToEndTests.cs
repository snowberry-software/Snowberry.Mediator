using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

public class EndToEndTests
{
    [Fact]
    public void Test_RequestHandler_Dispatches()
    {
        const string body = """
            public sealed class AddRequest : IRequest<AddRequest, int> { public int Value { get; set; } }

            public sealed class AddHandler : IRequestHandler<AddRequest, int>
            {
                public ValueTask<int> HandleAsync(AddRequest request, CancellationToken cancellationToken = default) => new(request.Value + 1);
            }

            public static class Runner
            {
                public static string Run()
                {
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    return mediator.SendAsync<AddRequest, int>(new AddRequest { Value = 41 }).AsTask().GetAwaiter().GetResult().ToString();
                }
            }
            """;

        Assert.Equal("42", Scenarios.Run(body));
    }

    [Fact]
    public void Test_PipelineBehaviors_ExecuteInPriorityOrder()
    {
        const string body = """
            public sealed class OrderRequest : IRequest<OrderRequest, string> { }

            public sealed class OrderHandler : IRequestHandler<OrderRequest, string>
            {
                public ValueTask<string> HandleAsync(OrderRequest request, CancellationToken cancellationToken = default) => new("H");
            }

            [PipelineOverwritePriority(Priority = 10)]
            public sealed class HighBehavior : IPipelineBehavior<OrderRequest, string>
            {
                public async ValueTask<string> HandleAsync<TNext>(OrderRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<OrderRequest, string>
                    => "High|" + await next.InvokeAsync(request, cancellationToken);
            }

            [PipelineOverwritePriority(Priority = 5)]
            public sealed class LowBehavior : IPipelineBehavior<OrderRequest, string>
            {
                public async ValueTask<string> HandleAsync<TNext>(OrderRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<OrderRequest, string>
                    => "Low|" + await next.InvokeAsync(request, cancellationToken);
            }

            public static class Runner
            {
                public static string Run()
                {
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    return mediator.SendAsync<OrderRequest, string>(new OrderRequest()).AsTask().GetAwaiter().GetResult();
                }
            }
            """;

        // Higher priority runs first (outermost).
        Assert.Equal("High|Low|H", Scenarios.Run(body));
    }

    [Fact]
    public void Test_OpenGenericPipelineBehavior_AppliesToRequest()
    {
        const string body = """
            public sealed class GenRequest : IRequest<GenRequest, string> { }

            public sealed class GenHandler : IRequestHandler<GenRequest, string>
            {
                public ValueTask<string> HandleAsync(GenRequest request, CancellationToken cancellationToken = default) => new("H");
            }

            public sealed class WrapAll<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : class, IRequest<TRequest, TResponse>
            {
                public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<TRequest, TResponse>
                {
                    Sink.Items.Add("Wrap");
                    return await next.InvokeAsync(request, cancellationToken);
                }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    var result = mediator.SendAsync<GenRequest, string>(new GenRequest()).AsTask().GetAwaiter().GetResult();
                    return string.Join(",", Sink.Items) + "=" + result;
                }
            }
            """;

        Assert.Equal("Wrap=H", Scenarios.Run(body));
    }

    [Fact]
    public void Test_StreamRequestHandler_StreamsValues()
    {
        const string body = """
            public sealed class NumberStream : IStreamRequest<NumberStream, int> { }

            public sealed class NumberStreamHandler : IStreamRequestHandler<NumberStream, int>
            {
                public async IAsyncEnumerable<int> HandleAsync(NumberStream request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
                {
                    yield return 1;
                    yield return 2;
                    yield return 3;
                    await Task.CompletedTask;
                }
            }

            public static class Runner
            {
                public static string Run() => RunAsync().GetAwaiter().GetResult();

                private static async Task<string> RunAsync()
                {
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    var items = new List<int>();
                    await foreach (var value in mediator.CreateStreamAsync<NumberStream, int>(new NumberStream()))
                        items.Add(value);
                    return string.Join(",", items);
                }
            }
            """;

        Assert.Equal("1,2,3", Scenarios.Run(body));
    }

    [Fact]
    public void Test_MultipleNotificationHandlers_AllRun()
    {
        const string body = """
            public sealed class Ping : INotification { }

            public sealed class PingA : INotificationHandler<Ping>
            {
                public ValueTask HandleAsync(Ping notification, CancellationToken cancellationToken = default) { Sink.Items.Add("A"); return default; }
            }

            public sealed class PingB : INotificationHandler<Ping>
            {
                public ValueTask HandleAsync(Ping notification, CancellationToken cancellationToken = default) { Sink.Items.Add("B"); return default; }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    mediator.PublishAsync(new Ping()).AsTask().GetAwaiter().GetResult();
                    Sink.Items.Sort();
                    return string.Join(",", Sink.Items);
                }
            }
            """;

        Assert.Equal("A,B", Scenarios.Run(body));
    }

    [Fact]
    public void Test_OpenGenericNotificationHandler_RunsBeforeConcrete()
    {
        const string body = """
            public sealed class Evt : INotification { }

            public sealed class EvtHandler : INotificationHandler<Evt>
            {
                public ValueTask HandleAsync(Evt notification, CancellationToken cancellationToken = default) { Sink.Items.Add("Concrete"); return default; }
            }

            public sealed class AuditAll<T> : INotificationHandler<T> where T : INotification
            {
                public ValueTask HandleAsync(T notification, CancellationToken cancellationToken = default) { Sink.Items.Add("Audit"); return default; }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    mediator.PublishAsync(new Evt()).AsTask().GetAwaiter().GetResult();
                    return string.Join(",", Sink.Items);
                }
            }
            """;

        // Open-generic (flattened) handlers run before concrete handlers within a notification group.
        Assert.Equal("Audit,Concrete", Scenarios.Run(body));
    }

    [Fact]
    public void Test_EmptyResponse_Dispatches()
    {
        const string body = """
            public sealed class Fire : IRequest<Fire, Empty> { }

            public sealed class FireHandler : IRequestHandler<Fire, Empty>
            {
                public ValueTask<Empty> HandleAsync(Fire request, CancellationToken cancellationToken = default)
                {
                    Sink.Items.Add("fired");
                    return Empty.ValueTask;
                }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    mediator.SendAsync<Fire, Empty>(new Fire()).AsTask().GetAwaiter().GetResult();
                    return string.Join(",", Sink.Items);
                }
            }
            """;

        Assert.Equal("fired", Scenarios.Run(body));
    }

    [Fact]
    public void Test_RecordRequest_Dispatches()
    {
        const string body = """
            public sealed record RecordRequest(int Value) : IRequest<RecordRequest, int>;

            public sealed class RecordHandler : IRequestHandler<RecordRequest, int>
            {
                public ValueTask<int> HandleAsync(RecordRequest request, CancellationToken cancellationToken = default) => new(request.Value * 2);
            }

            public static class Runner
            {
                public static string Run()
                {
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    return mediator.SendAsync<RecordRequest, int>(new RecordRequest(21)).AsTask().GetAwaiter().GetResult().ToString();
                }
            }
            """;

        Assert.Equal("42", Scenarios.Run(body));
    }

    [Fact]
    public void Test_ConcreteAndOpenGenericBehaviors_OrderedByPriority()
    {
        const string body = """
            public sealed class MixRequest : IRequest<MixRequest, string> { }

            public sealed class MixHandler : IRequestHandler<MixRequest, string>
            {
                public ValueTask<string> HandleAsync(MixRequest request, CancellationToken cancellationToken = default) => new("H");
            }

            [PipelineOverwritePriority(Priority = 100)]
            public sealed class SpecificBehavior : IPipelineBehavior<MixRequest, string>
            {
                public async ValueTask<string> HandleAsync<TNext>(MixRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<MixRequest, string>
                { Sink.Items.Add("Specific"); return await next.InvokeAsync(request, cancellationToken); }
            }

            [PipelineOverwritePriority(Priority = 1)]
            public sealed class GenericLow<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : class, IRequest<TRequest, TResponse>
            {
                public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<TRequest, TResponse>
                { Sink.Items.Add("GenericLow"); return await next.InvokeAsync(request, cancellationToken); }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    var result = mediator.SendAsync<MixRequest, string>(new MixRequest()).AsTask().GetAwaiter().GetResult();
                    return string.Join(",", Sink.Items) + "=" + result;
                }
            }
            """;

        // Priority 100 (specific) before priority 1 (open generic).
        Assert.Equal("Specific,GenericLow=H", Scenarios.Run(body));
    }

    [Fact]
    public void Test_SingletonLifetime_Works()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using Snowberry.Mediator.Abstractions;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace E2E;

            public sealed class LifeRequest : IRequest<LifeRequest, int> { }
            public sealed class LifeHandler : IRequestHandler<LifeRequest, int>
            {
                public ValueTask<int> HandleAsync(LifeRequest request, CancellationToken cancellationToken = default) => new(7);
            }

            public static class Runner
            {
                public static string Run()
                {
                    var mediator = new ServiceCollection().AddSnowberryMediator(ServiceLifetime.Singleton).BuildServiceProvider().GetRequiredService<IMediator>();
                    return mediator.SendAsync<LifeRequest, int>(new LifeRequest()).AsTask().GetAwaiter().GetResult().ToString();
                }
            }
            """;

        var result = GeneratorTestHelper.Run(source);
        Scenarios.AssertNoErrors(result);
        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("E2E.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        Assert.Equal("7", value);
    }

    [Fact]
    public void Test_CategoryToggle_DisablesPipelineBehaviors()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using Snowberry.Mediator.Abstractions;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;
            using Snowberry.Mediator.Abstractions.Pipeline;

            [assembly: Snowberry.Mediator.SnowberryMediator(RegisterPipelineBehaviors = false)]

            namespace E2E;

            public static class Sink { public static readonly List<string> Items = new(); }

            public sealed class ToggleRequest : IRequest<ToggleRequest, string> { }
            public sealed class ToggleHandler : IRequestHandler<ToggleRequest, string>
            {
                public ValueTask<string> HandleAsync(ToggleRequest request, CancellationToken cancellationToken = default) => new("H");
            }
            public sealed class ToggleBehavior : IPipelineBehavior<ToggleRequest, string>
            {
                public async ValueTask<string> HandleAsync<TNext>(ToggleRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<ToggleRequest, string>
                { Sink.Items.Add("Behavior"); return await next.InvokeAsync(request, cancellationToken); }
            }

            public static class Runner
            {
                public static string Run()
                {
                    Sink.Items.Clear();
                    var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                    var result = mediator.SendAsync<ToggleRequest, string>(new ToggleRequest()).AsTask().GetAwaiter().GetResult();
                    return string.Join(",", Sink.Items) + "=" + result;
                }
            }
            """;

        var result = GeneratorTestHelper.Run(source);
        Scenarios.AssertNoErrors(result);

        // The behavior must NOT be registered, so it never runs.
        Assert.DoesNotContain("ToggleBehavior", result.RegistrationSource);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("E2E.Runner")!.GetMethod("Run")!.Invoke(null, null)!;
        Assert.Equal("=H", value);
    }
}

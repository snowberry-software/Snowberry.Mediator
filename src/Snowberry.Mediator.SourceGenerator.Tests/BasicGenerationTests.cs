using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

public class BasicGenerationTests
{
    private const string SimpleSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public sealed class CounterRequest : IRequest<CounterRequest, int>
        {
            public int Value { get; set; }
        }

        public sealed class CounterHandler : IRequestHandler<CounterRequest, int>
        {
            public ValueTask<int> HandleAsync(CounterRequest request, CancellationToken cancellationToken = default)
                => new(request.Value + 1);
        }

        public static class Runner
        {
            public static int Run()
            {
                var services = new ServiceCollection();
                services.AddSnowberryMediator();
                var provider = services.BuildServiceProvider();
                var mediator = provider.GetRequiredService<IMediator>();
                return mediator.SendAsync<CounterRequest, int>(new CounterRequest { Value = 41 }).AsTask().GetAwaiter().GetResult();
            }
        }
        """;

    [Fact]
    public void Test_RequestHandler_GeneratesAndDispatches()
    {
        // Act
        var result = GeneratorTestHelper.Run(SimpleSource);

        // Assert: no errors from the generated source.
        Assert.DoesNotContain(result.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var runner = assembly.GetType("E2E.Runner")!;
        var value = (int)runner.GetMethod("Run")!.Invoke(null, null)!;

        Assert.Equal(42, value);
    }

    [Fact]
    public void Test_GeneratedSource_RegistersClosedHandlerWithoutReflection()
    {
        var result = GeneratorTestHelper.Run(SimpleSource);
        var source = result.RegistrationSource;

        Assert.NotNull(source);
        Assert.Contains("typeof(global::Snowberry.Mediator.Abstractions.Handler.IRequestHandler<global::E2E.CounterRequest, int>)", source);
        Assert.Contains("typeof(global::E2E.CounterHandler)", source);
        Assert.DoesNotContain("MakeGenericType", source);
        Assert.DoesNotContain("GetTypes", source);
    }

    [Fact]
    public void Test_NoTriggerAttribute_GeneratesNothing()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            namespace E2E;

            public sealed class PingRequest : IRequest<PingRequest, int> { }

            public sealed class PingHandler : IRequestHandler<PingRequest, int>
            {
                public ValueTask<int> HandleAsync(PingRequest request, CancellationToken cancellationToken = default) => new(1);
            }
            """;

        var result = GeneratorTestHelper.Run(source);

        Assert.Null(result.RegistrationSource);
    }
}

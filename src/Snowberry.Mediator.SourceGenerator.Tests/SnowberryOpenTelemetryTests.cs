using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>
/// Verifies the generated Snowberry.DependencyInjection entry point composes with the OpenTelemetry decorator.
/// When AddSnowberryMediatorOpenTelemetry runs first, the generated AddSnowberryMediator registers IMediator with
/// a try-add, so the decorator keeps the slot and dispatch still reaches the generated-registered handler.
/// </summary>
public class SnowberryOpenTelemetryTests
{
    private const string c_Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.DependencyInjection;
        using Snowberry.DependencyInjection.Abstractions.Extensions;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;
        using Snowberry.Mediator.DependencyInjection;
        using Snowberry.Mediator.OpenTelemetry;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public sealed class OtReq : IRequest<OtReq, int> { }

        public sealed class OtHandler : IRequestHandler<OtReq, int>
        {
            public ValueTask<int> HandleAsync(OtReq request, CancellationToken cancellationToken = default) => new(7);
        }

        public static class Runner
        {
            public static string Run()
            {
                using var container = new ServiceContainer();
                container.AddSnowberryMediatorOpenTelemetry();   // decorator wins the IMediator slot first
                container.AddSnowberryMediator();                // generated; IMediator TryRegister is a no-op

                var mediator = container.GetRequiredService<IMediator>();
                int result = mediator.SendAsync<OtReq, int>(new OtReq()).AsTask().GetAwaiter().GetResult();
                return mediator.GetType().Name + ":" + result;
            }
        }
        """;

    [Fact]
    public void Test_GeneratedSnowberryEntryPoint_ComposesWithOpenTelemetryDecorator()
    {
        var result = GeneratorTestHelper.Run(c_Source);

        Assert.DoesNotContain(result.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("E2E.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        // Decorator kept the IMediator slot, and dispatch reached the generated-registered handler.
        Assert.Equal("InstrumentedMediator:7", value);
    }
}

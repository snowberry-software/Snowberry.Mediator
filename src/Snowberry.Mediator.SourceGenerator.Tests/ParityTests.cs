namespace Snowberry.Mediator.SourceGenerator.Tests;

public class ParityTests
{
    /// <summary>
    /// The generated (reflection-free) registration must produce identical dispatch and ordering to the
    /// existing reflection-based <c>AddSnowberryMediator(Action&lt;MediatorOptions&gt;)</c> path.
    /// </summary>
    [Fact]
    public void Test_GeneratedAndReflection_ProduceIdenticalResults()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using Snowberry.Mediator;
            using Snowberry.Mediator.Abstractions;
            using Snowberry.Mediator.Abstractions.Attributes;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;
            using Snowberry.Mediator.Abstractions.Pipeline;
            using Snowberry.Mediator.Extensions.DependencyInjection;

            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace App;

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
                    var generated = new ServiceCollection()
                        .AddSnowberryMediator()
                        .BuildServiceProvider()
                        .GetRequiredService<IMediator>();
                    var generatedResult = generated.SendAsync<OrderRequest, string>(new OrderRequest()).AsTask().GetAwaiter().GetResult();

                    var reflectionServices = new ServiceCollection();
                    reflectionServices.AddSnowberryMediator(options =>
                    {
                        options.RequestHandlerTypes = new List<System.Type> { typeof(OrderHandler) };
                        options.PipelineBehaviorTypes = new List<System.Type> { typeof(HighBehavior), typeof(LowBehavior) };
                    });
                    var reflection = reflectionServices.BuildServiceProvider().GetRequiredService<IMediator>();
                    var reflectionResult = reflection.SendAsync<OrderRequest, string>(new OrderRequest()).AsTask().GetAwaiter().GetResult();

                    return generatedResult + " / " + reflectionResult + " / " + (generatedResult == reflectionResult);
                }
            }
            """;

        var result = GeneratorTestHelper.Run(source);
        Scenarios.AssertNoErrors(result);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("App.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        Assert.Equal("High|Low|H / High|Low|H / True", value);
    }
}

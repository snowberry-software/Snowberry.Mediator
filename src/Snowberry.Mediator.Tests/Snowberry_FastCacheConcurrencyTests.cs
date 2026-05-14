using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Stress-tests the Tier 3b <c>PipelineFastCache&lt;TRequest, TResponse&gt;</c> by running two
/// independently-configured mediators in parallel against the same <see cref="CounterRequest"/> type.
/// The fast cache is keyed by <c>(TRequest, TResponse)</c> alone - but identifies its owner registry
/// via reference identity + generation. Alternating dispatches must thrash the cache and still
/// produce the correct response for each mediator's configured behaviors.
/// </summary>
public class Snowberry_FastCacheConcurrencyTests
{
    [Fact]
    public async Task PipelineFastCache_Thrashes_Correctly_Across_Two_Registries()
    {
        using var containerA = new ServiceContainer();
        containerA.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            options.PipelineBehaviorTypes = [typeof(MediumPriorityCounterRequestPipelineBehavior)];
        }, serviceLifetime: ServiceLifetime.Singleton);
        var mediatorA = containerA.GetRequiredService<IMediator>();

        using var containerB = new ServiceContainer();
        containerB.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            options.PipelineBehaviorTypes = [typeof(LowPriorityCounterRequestPipelineBehavior)];
        }, serviceLifetime: ServiceLifetime.Singleton);
        var mediatorB = containerB.GetRequiredService<IMediator>();

        // Handler returns InitialValue (5). Medium adds 10 → 15. Low adds 100 → 105.
        // Warm up to ensure each mediator builds its own fast-cache entry at least once.
        Assert.Equal(15, await mediatorA.SendAsync(new CounterRequest(), CancellationToken.None));
        Assert.Equal(105, await mediatorB.SendAsync(new CounterRequest(), CancellationToken.None));

        const int Iterations = 1000;
        var resultsA = new int[Iterations];
        var resultsB = new int[Iterations];

        await Task.Run(() => Parallel.For(0, Iterations, i =>
        {
            // Each iteration alternates which mediator handles the request, forcing constant cache thrash.
            resultsA[i] = mediatorA.SendAsync(new CounterRequest(), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            resultsB[i] = mediatorB.SendAsync(new CounterRequest(), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }));

        for (int i = 0; i < Iterations; i++)
        {
            Assert.Equal(15, resultsA[i]);
            Assert.Equal(105, resultsB[i]);
        }
    }
}
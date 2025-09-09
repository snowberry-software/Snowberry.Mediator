using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Tests focused on complex priority scenarios, ordering, and edge cases with pipeline behaviors
/// </summary>
public class PipelinePriorityComplexTests : Common.MediatorTestBase
{
    [Theory]
    [InlineData(-1000, -500, -100, 0, 100, 500, 1000)]
    [InlineData(int.MinValue, -1, 0, 1, int.MaxValue)]
    [InlineData(50, 40, 30, 20, 10)]
    public async Task Test_Priority_ExtremeCases(params int[] priorities)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(PriorityTestRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new PriorityTestRequest { Message = "PriorityTest" };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Equal("Handled: PriorityTest", response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Empty(executionOrder);
        Assert.NotNull(priorities);
    }

    [Fact]
    public async Task Test_MixedPriorityTypes_WithSamePriority()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SamePriorityBehaviorA).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(SamePriorityBehaviorA),     // Priority 100
                typeof(SamePriorityBehaviorB),     // Priority 100 
                typeof(SamePriorityBehaviorC),     // Priority 100
                typeof(NoPriorityBehaviorA),       // Priority 0 (default)
                typeof(NoPriorityBehaviorB)        // Priority 0 (default)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new MultiBehaviorRequest { Value = 10 };
        int response = await mediator.SendAsync(request, CancellationToken.None);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(5, executionOrder.Count);

        var first3 = executionOrder.Take(3).ToList();
        Assert.Contains(nameof(SamePriorityBehaviorA), first3);
        Assert.Contains(nameof(SamePriorityBehaviorB), first3);
        Assert.Contains(nameof(SamePriorityBehaviorC), first3);

        var last2 = executionOrder.Skip(3).Take(2).ToList();
        Assert.Contains(nameof(NoPriorityBehaviorA), last2);
        Assert.Contains(nameof(NoPriorityBehaviorB), last2);
    }

    [Fact]
    public async Task Test_DeepPipelineNesting_Performance()
    {
        var serviceCollection = new ServiceCollection();

        for (int i = 0; i < 10; i++)
        {
            int priority = 1000 - (i * 100);
            serviceCollection.AddScoped(sp =>
                new PerformancePipelineBehavior($"Behavior{i:D2}", priority));
        }

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(PerformanceTestRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var startTime = DateTime.UtcNow;

        var request = new PerformanceTestRequest { BaseValue = 1 };
        int response = await mediator.SendAsync(request, CancellationToken.None);

        var duration = DateTime.UtcNow - startTime;

        Assert.True(duration.TotalMilliseconds < 1000, $"Request took too long: {duration.TotalMilliseconds}ms");
        Assert.Equal(1, response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Empty(executionOrder);
    }

    [Fact]
    public async Task Test_PriorityOverride_WithInheritance()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(BasePipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(BasePipelineBehavior),      // Priority 0
                typeof(DerivedPipelineBehavior),   // Priority 200 (overrides base)
                typeof(GrandChildPipelineBehavior) // Priority 300 (overrides derived)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new InheritanceTestRequest { Data = "test" };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(3, executionOrder.Count);

        Assert.Equal(nameof(GrandChildPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(DerivedPipelineBehavior), executionOrder[1]);
        Assert.Equal(nameof(BasePipelineBehavior), executionOrder[2]);
    }

    [Fact]
    public async Task Test_StreamPriority_WithComplexTransformations()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(MultiplyStreamBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [
                typeof(AddStreamBehavior),           // Priority 10
                typeof(MultiplyStreamBehavior),      // Priority 50
                typeof(FilterStreamBehavior),        // Priority 100 (highest)
                typeof(LoggingStreamBehavior)        // Priority 0
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new NumberStreamRequest { Count = 10, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([12, 14, 16, 18, 20, 22, 24, 26, 28, 30], results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);

        Assert.Equal(nameof(FilterStreamBehavior), executionOrder[0]);
        Assert.Equal(nameof(MultiplyStreamBehavior), executionOrder[1]);
        Assert.Equal(nameof(AddStreamBehavior), executionOrder[2]);
        Assert.Equal(nameof(LoggingStreamBehavior), executionOrder[3]);
    }
}
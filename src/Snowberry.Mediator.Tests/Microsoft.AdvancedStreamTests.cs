using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Advanced tests for streaming functionality, including complex transformations and edge cases
/// </summary>
public class Microsoft_AdvancedStreamTests : Common.MediatorTestBase
{
    [Fact]
    public async Task Test_MultipleStreamPipelines_ChainedTransformations()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ChainedStreamBehavior1).Assembly];
            options.StreamPipelineBehaviorTypes = [
                typeof(ChainedStreamBehavior1),    // Priority 300 - First execution (*10)
                typeof(ChainedStreamBehavior2),    // Priority 200 - Second (+100) 
                typeof(ChainedStreamBehavior3),    // Priority 100 - Third (*2)
                typeof(ChainedStreamBehavior4)     // Priority 0 - Last (+1)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new NumberStreamRequest { Count = 5, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        int[] expected = new[] { 1040, 1060, 1080, 1100, 1120 };
        Assert.Equal(expected, results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal(nameof(ChainedStreamBehavior1), executionOrder[0]);
        Assert.Equal(nameof(ChainedStreamBehavior2), executionOrder[1]);
        Assert.Equal(nameof(ChainedStreamBehavior3), executionOrder[2]);
        Assert.Equal(nameof(ChainedStreamBehavior4), executionOrder[3]);
    }

    [Fact]
    public async Task Test_MixedConcreteAndGenericPipelineBehaviors()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(MixedPipelineTestRequest).Assembly];
            options.PipelineBehaviorTypes = [
                // Concrete behaviors with specific priorities
                typeof(HighPriorityConcretePipelineBehavior),    // Priority 200 - Highest
                typeof(LowPriorityConcretePipelineBehavior),     // Priority 25 - Lowest
                
                // Open generic behaviors with different priorities
                typeof(HighPriorityGenericPipelineBehavior<,>), // Priority 150 - Second highest
                typeof(MediumPriorityGenericPipelineBehavior<,>) // Priority 75 - Second lowest
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new MixedPipelineTestRequest { Message = "Test", Value = 42 };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        // Expected execution order by priority (highest to lowest):
        // 1. HighPriorityConcretePipelineBehavior (200)
        // 2. HighPriorityGenericPipelineBehavior (150) 
        // 3. MediumPriorityGenericPipelineBehavior (75)
        // 4. LowPriorityConcretePipelineBehavior (25)

        // Expected response with nested wrappers:
        // [H:[L:Handled:Test:42]]
        Assert.Equal("[H:[L:Handled:Test:42]]", response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal(nameof(HighPriorityConcretePipelineBehavior), executionOrder[0]);
        Assert.Equal("HighPriorityGenericPipelineBehavior<MixedPipelineTestRequest, String>", executionOrder[1]);
        Assert.Equal("MediumPriorityGenericPipelineBehavior<MixedPipelineTestRequest, String>", executionOrder[2]);
        Assert.Equal(nameof(LowPriorityConcretePipelineBehavior), executionOrder[3]);
    }

    [Fact]
    public async Task Test_MixedConcreteAndGenericStreamPipelineBehaviors()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(MixedStreamPipelineTestRequest).Assembly];
            options.StreamPipelineBehaviorTypes = [
                // Concrete stream behaviors with specific priorities
                typeof(HighPriorityConcreteStreamPipelineBehavior),    // Priority 150 - Highest
                typeof(LowPriorityConcreteStreamPipelineBehavior),     // Priority 30 - Lowest
                
                // Open generic stream behaviors with different priorities  
                typeof(HighPriorityGenericStreamPipelineBehavior<,>),  // Priority 120 - Second highest
                typeof(MediumPriorityGenericStreamPipelineBehavior<,>) // Priority 80 - Second lowest
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new MixedStreamPipelineTestRequest { Count = 3, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([1002, 1004, 1006], results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);

        // Actual execution order (by priority - highest to lowest):
        Assert.Equal(nameof(HighPriorityConcreteStreamPipelineBehavior), executionOrder[0]);
        Assert.Equal("HighPriorityGenericStreamPipelineBehavior<MixedStreamPipelineTestRequest, Int32>", executionOrder[1]);
        Assert.Equal("MediumPriorityGenericStreamPipelineBehavior<MixedStreamPipelineTestRequest, Int32>", executionOrder[2]);
        Assert.Equal(nameof(LowPriorityConcreteStreamPipelineBehavior), executionOrder[3]);
    }

    [Fact]
    public async Task Test_StreamRequest_WithComplexData()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ComplexDataStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new ComplexDataStreamRequest
        {
            StartDate = DateTime.UtcNow.Date,
            Count = 7,
            Prefix = "Week"
        };

        var results = new List<ComplexDataItem>();

        await foreach (var item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal(7, results.Count);
        Assert.All(results, item => Assert.StartsWith("Week", item.Name));

        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(request.StartDate.AddDays(i), results[i].Date);
        }
    }

    [Fact]
    public async Task Test_StreamRequest_AsyncDisposalPattern()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(DisposableStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new DisposableStreamRequest { ResourceCount = 10 };
        int processedCount = 0;

        await using var enumerator = mediator.CreateStreamAsync(request, CancellationToken.None).GetAsyncEnumerator();

        for (int i = 0; i < 3; i++)
        {
            Assert.True(await enumerator.MoveNextAsync());
            processedCount++;
            Assert.Equal($"Resource{i + 1}", enumerator.Current.Name);
        }

        await enumerator.DisposeAsync();

        Assert.Equal(3, processedCount);
    }

    [Fact]
    public async Task Test_StreamRequest_BackpressureScenario()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(BackpressureBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [typeof(BackpressureBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new NumberStreamRequest { Count = 100, StartValue = 1 };
        var startTime = DateTime.UtcNow;
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
            await Task.Delay(1);

            if (results.Count >= 20)
                break;
        }

        var duration = DateTime.UtcNow - startTime;

        Assert.Equal(20, results.Count);
        Assert.True(duration.TotalMilliseconds >= 20);
        Assert.True(duration.TotalMilliseconds < 5000);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(BackpressureBehavior), executionOrder[0]);
    }

    [Fact]
    public async Task Test_ConcurrentStreamRequests()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Singleton);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var tasks = new List<Task<List<int>>>();

        for (int i = 0; i < 10; i++)
        {
            int streamIndex = i;
            var request = new NumberStreamRequest { Count = 5, StartValue = (streamIndex * 10) + 1 };

            tasks.Add(Task.Run(async () =>
            {
                var results = new List<int>();
                await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
                {
                    results.Add(item);
                }

                return results;
            }));
        }

        var allResults = await Task.WhenAll(tasks);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(5, allResults[i].Count);
            int expectedStart = (i * 10) + 1;
            Assert.Equal(Enumerable.Range(expectedStart, 5), allResults[i]);
        }

        int[] flatResults = allResults.SelectMany(r => r).ToArray();
        int[] uniqueResults = flatResults.Distinct().ToArray();
        Assert.Equal(flatResults.Length, uniqueResults.Length);
    }

    [Fact]
    public async Task Test_StreamRequest_FilteringPipeline()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ConditionalFilterBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [typeof(ConditionalFilterBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new FilterableStreamRequest
        {
            Count = 20,
            StartValue = 1,
            FilterCondition = x => x % 3 == 0 || x > 15
        };

        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        int[] expected = new[] { 3, 6, 9, 12, 15, 16, 17, 18, 19, 20 };
        Assert.Equal(expected, results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(ConditionalFilterBehavior), executionOrder[0]);
    }

    [Fact]
    public async Task Test_StreamRequest_ExceptionRecovery()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ExceptionRecoveryBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [typeof(ExceptionRecoveryBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        using var serviceProvider = serviceCollection.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var request = new FaultyStreamRequest { Count = 10, FaultAtPositions = [3, 7] };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal(10, results.Count);
        Assert.Equal(-1, results[2]);
        Assert.Equal(-1, results[6]);

        for (int i = 0; i < 10; i++)
        {
            if (i != 2 && i != 6)
            {
                Assert.Equal(i + 1, results[i]);
            }
        }

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(ExceptionRecoveryBehavior), executionOrder[0]);
    }
}
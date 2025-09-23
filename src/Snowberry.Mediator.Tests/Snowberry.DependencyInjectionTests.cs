using Snowberry.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

public class Snowberry_DependencyInjectionTests : MediatorTestBase
{
    [Fact]
    public async Task Test_DependencyInjection_Order()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(CounterRequestPipelineBehavior),
                typeof(AlwaysFirstCounterRequestPipelineBehavior)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        Assert.IsType<Mediator>(mediator);

        int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);

        Assert.Equal(CounterRequest.c_InitialValue + 2, response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();

        Assert.NotEmpty(executionOrder);
        Assert.Equal(2, executionOrder.Count);

        Assert.Equal(nameof(AlwaysFirstCounterRequestPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(CounterRequestPipelineBehavior), executionOrder[1]);
    }

    [Fact]
    public async Task Test_PipelineBehavior_Priority_Override()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(LowPriorityCounterRequestPipelineBehavior),     // Priority 10
                typeof(CounterRequestPipelineBehavior),                // No priority (0)
                typeof(MediumPriorityCounterRequestPipelineBehavior),  // Priority 50
                typeof(AlwaysFirstCounterRequestPipelineBehavior)      // Priority int.MaxValue
            ];
        }, serviceLifetime: ServiceLifetime.Transient);

        var mediator = serviceContainer.GetService<IMediator>();

        int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);

        Assert.Equal(117, response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal(nameof(AlwaysFirstCounterRequestPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(MediumPriorityCounterRequestPipelineBehavior), executionOrder[1]);
        Assert.Equal(nameof(LowPriorityCounterRequestPipelineBehavior), executionOrder[2]);
        Assert.Equal(nameof(CounterRequestPipelineBehavior), executionOrder[3]);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task Test_DifferentServiceLifetimes(ServiceLifetime lifetime)
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(CounterRequestPipelineBehavior),
                typeof(AlwaysFirstCounterRequestPipelineBehavior)
            ];
        }, serviceLifetime: lifetime);

        for (int i = 0; i < 3; i++)
        {
            using var scope = serviceContainer.CreateScope();
            var mediator = scope.ServiceFactory.GetService<IMediator>();

            int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);
            Assert.Equal(CounterRequest.c_InitialValue + 2, response);
        }

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(6, executionOrder.Count);
    }

    [Fact]
    public async Task Test_StreamPipelineBehaviors_Execution_Order()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(HighPriorityStreamPipelineBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [
                typeof(BasicStreamPipelineBehavior),      // No priority (0)
                typeof(HighPriorityStreamPipelineBehavior) // Priority 100
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 3, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([2002, 2004, 2006], results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal(nameof(HighPriorityStreamPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(BasicStreamPipelineBehavior), executionOrder[1]);
    }

    [Fact]
    public async Task Test_ComplexRequest_WithPipeline()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ComplexRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [typeof(ComplexRequestPipelineBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new ComplexRequest { Message = "Test", Factor = 3 };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Equal("[Test x3]", response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(ComplexRequestPipelineBehavior), executionOrder[0]);
    }

    [Fact]
    public async Task Test_OpenGeneric_PipelineBehaviors_Documentation()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(CounterRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        int counterResponse = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);
        Assert.Equal(CounterRequest.c_InitialValue, counterResponse);

        string complexResponse = await mediator.SendAsync(new ComplexRequest { Message = "Generic", Factor = 2 }, CancellationToken.None);
        Assert.Equal("Generic x2", complexResponse);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Empty(executionOrder);
    }

    [Fact]
    public async Task Test_OpenGeneric_StreamPipelineBehaviors()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 2, StartValue = 10 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([10, 11], results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Empty(executionOrder);
    }

    [Fact]
    public async Task Test_MultiplePipelineBehaviors_ComplexScenario()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(LowPriorityCounterRequestPipelineBehavior),    // Priority 10
                typeof(CounterRequestPipelineBehavior),               // No priority = 0
                typeof(MediumPriorityCounterRequestPipelineBehavior), // Priority 50
                typeof(AlwaysFirstCounterRequestPipelineBehavior)     // Priority MaxValue
            ];
        }, serviceLifetime: ServiceLifetime.Singleton);

        var mediator = serviceContainer.GetService<IMediator>();

        int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal(nameof(AlwaysFirstCounterRequestPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(MediumPriorityCounterRequestPipelineBehavior), executionOrder[1]);
        Assert.Equal(nameof(LowPriorityCounterRequestPipelineBehavior), executionOrder[2]);
        Assert.Equal(nameof(CounterRequestPipelineBehavior), executionOrder[3]);
    }

    [Fact]
    public async Task Test_CancelledCancellationToken_ThrowsOperationCanceledException()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await mediator.SendAsync(new CounterRequest(), cts.Token);
        });
    }

    [Fact]
    public async Task Test_StreamRequest_CancelledCancellationToken()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 100, StartValue = 1 };
        using var cts = new CancellationTokenSource();

        var results = new List<int>();
        var enumerator = mediator.CreateStreamAsync(request, cts.Token).GetAsyncEnumerator(cts.Token);

        try
        {
            Assert.True(await enumerator.MoveNextAsync());
            results.Add(enumerator.Current);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await enumerator.MoveNextAsync();
            });
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        Assert.Single(results);
        Assert.Equal(1, results[0]);
    }

    [Fact]
    public async Task Test_MultipleStreamPipelineBehaviors_ComplexScenario()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(HighPriorityStreamPipelineBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [
                typeof(BasicStreamPipelineBehavior),              // No priority = 0
                typeof(HighPriorityStreamPipelineBehavior)        // Priority 100
            ];
        }, serviceLifetime: ServiceLifetime.Transient);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 2, StartValue = 5 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([2010, 2012], results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal(nameof(HighPriorityStreamPipelineBehavior), executionOrder[0]);
        Assert.Equal(nameof(BasicStreamPipelineBehavior), executionOrder[1]);
    }

    [Fact]
    public async Task Test_EmptyStreamRequest()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 0, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Empty(results);
    }

    [Fact]
    public async Task Test_LargeStreamRequest()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 50, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal(50, results.Count);
        Assert.Equal(1, results[0]);
        Assert.Equal(50, results[49]);
    }

    [Fact]
    public async Task Test_NoPipelineBehaviors_StillWorks()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(CounterRequest).Assembly];
            options.RegisterPipelineBehaviors = false;
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);

        Assert.Equal(CounterRequest.c_InitialValue, response);
    }

    [Fact]
    public async Task Test_NoStreamPipelineBehaviors_StillWorks()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
            options.RegisterStreamPipelineBehaviors = false;
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 3, StartValue = 10 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Equal([10, 11, 12], results);
    }

    [Fact]
    public async Task Test_MixedServiceLifetimes_WithComplexPipeline()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(AlwaysFirstCounterRequestPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [
                typeof(CounterRequestPipelineBehavior),
                typeof(MediumPriorityCounterRequestPipelineBehavior),
                typeof(LowPriorityCounterRequestPipelineBehavior)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        for (int scope = 1; scope <= 2; scope++)
        {
            using var scopedServiceProvider = serviceContainer.CreateScope();
            var mediator = scopedServiceProvider.ServiceFactory.GetService<IMediator>();

            int response = await mediator.SendAsync(new CounterRequest(), CancellationToken.None);

            Assert.Equal(116, response);
        }

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(6, executionOrder.Count);
    }

    [Fact]
    public async Task Test_StreamRequest_WithTimeout()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 5, StartValue = 1 };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var results = new List<int>();

        try
        {
            await foreach (int item in mediator.CreateStreamAsync(request, cts.Token))
            {
                results.Add(item);
                await Task.Delay(10, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }

        Assert.NotEmpty(results);
        Assert.True(results.Count <= 5);
        Assert.Equal(1, results[0]);
    }
}

using Snowberry.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

public class Snowberry_CancellationAndTimeoutTests : Common.MediatorTestBase
{
    [Fact]
    public async Task Test_TaskCancelledException_ThrownFromHandler()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(CancellationThrowingRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new CancellationThrowingRequest();

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await mediator.SendAsync(request, cts.Token);
        });

        Assert.NotNull(exception);
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task Test_StreamRequest_TaskCancelledException_MidStream()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(CancellationThrowingStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new CancellationThrowingStreamRequest { ThrowAfterCount = 3 };
        var results = new List<int>();

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
            {
                results.Add(item);
            }
        });

        Assert.Equal(3, results.Count);
        Assert.Equal([1, 2, 3], results);
    }

    [Fact]
    public async Task Test_PipelineBehavior_CancellationToken_Propagation()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(CancellationCheckingPipelineBehavior).Assembly];
            options.PipelineBehaviorTypes = [typeof(CancellationCheckingPipelineBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        using var cts = new CancellationTokenSource();
        var request = new DelayedRequest { DelayMs = 50 };

        var task = mediator.SendAsync(request, cts.Token);

        await Task.Delay(10);
        cts.Cancel();

        // Accept both OperationCanceledException and TaskCanceledException
        // TaskCanceledException is derived from OperationCanceledException
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(task.AsTask);

        // Verify the cancellation was properly propagated
        Assert.True(cts.Token.IsCancellationRequested);
        Assert.True(exception.CancellationToken.IsCancellationRequested);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Contains("CancellationCheckingPipelineBehavior", executionOrder);
    }

    [Fact]
    public async Task Test_MultipleRequests_ConcurrentCancellation()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(DelayedRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Singleton);

        var mediator = serviceContainer.GetService<IMediator>();

        var tasks = new List<Task<string>>();
        var cancellationSources = new List<CancellationTokenSource>();

        for (int i = 0; i < 10; i++)
        {
            var cts = new CancellationTokenSource();
            cancellationSources.Add(cts);

            var request = new DelayedRequest { DelayMs = 100 + (i * 10), Message = $"Request{i}" };
            tasks.Add(mediator.SendAsync(request, cts.Token).AsTask());

            if (i % 3 == 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(20 + (i * 5));
                    cts.Cancel();
                });
            }
        }

        string[] results = await Task.WhenAll(tasks.Select(async task =>
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                return "CANCELLED";
            }
        }));

        Assert.Contains("CANCELLED", results);
        Assert.Contains(results, r => r != "CANCELLED" && r.StartsWith("Delayed:Request"));

        cancellationSources.ForEach(cts => cts.Dispose());
    }

    [Fact]
    public async Task Test_StreamRequest_ComplexCancellationScenario()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SlowStreamPipelineBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [typeof(SlowStreamPipelineBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var request = new NumberStreamRequest { Count = 20, StartValue = 1 };
        var results = new List<int>();
        int itemsProcessed = 0;

        var startTime = DateTime.UtcNow;

        try
        {
            await foreach (int item in mediator.CreateStreamAsync(request, cts.Token))
            {
                results.Add(item);
                itemsProcessed++;

                await Task.Delay(10, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }

        var duration = DateTime.UtcNow - startTime;

        Assert.True(duration.TotalMilliseconds < 300);
        Assert.True(itemsProcessed > 0, "Should have processed some items before cancellation");
        Assert.True(itemsProcessed < 20, "Should not have processed all items due to cancellation");

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Contains("SlowStreamPipelineBehavior", executionOrder);
    }

    [Fact]
    public async Task Test_NestedCancellationTokens()
    {
        using var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(DelayedRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        using var outerCts = new CancellationTokenSource();
        using var innerCts = new CancellationTokenSource();

        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            outerCts.Token, innerCts.Token);

        var request = new DelayedRequest { DelayMs = 200, Message = "NestedTest" };

        var task = mediator.SendAsync(request, combinedCts.Token);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            innerCts.Cancel();
        });

        // Accept both OperationCanceledException and TaskCanceledException for robust testing
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(task.AsTask);
        Assert.True(combinedCts.Token.IsCancellationRequested);
        Assert.True(innerCts.Token.IsCancellationRequested);
        Assert.False(outerCts.Token.IsCancellationRequested);
    }
}
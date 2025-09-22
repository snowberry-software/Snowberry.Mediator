using Snowberry.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Tests.Common;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Pipelines;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Tests focused on edge cases, error scenarios, and boundary conditions
/// </summary>
public class Snowberry_EdgeCasesAndErrorTests : MediatorTestBase
{
    [Fact]
    public async Task Test_Request_WithNullValues()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NullableRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NullableRequest { NullableString = null, RequiredString = "Required" };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Contains("null", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Required", response);
    }

    [Fact]
    public async Task Test_Handler_ThrowsCustomException()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ExceptionThrowingRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new ExceptionThrowingRequest { ShouldThrow = true, Message = "Test exception" };

        var exception = await Assert.ThrowsAsync<CustomBusinessException>(async () =>
        {
            await mediator.SendAsync(request, CancellationToken.None);
        });

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task Test_PipelineBehavior_ExceptionHandling()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ExceptionHandlingBehavior).Assembly];
            options.PipelineBehaviorTypes = [typeof(ExceptionHandlingBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new ExceptionThrowingRequest { ShouldThrow = true, Message = "Pipeline test" };

        string response = await mediator.SendAsync(request, CancellationToken.None);
        Assert.Equal("Exception caught: Pipeline test", response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(ExceptionHandlingBehavior), executionOrder[0]);
    }

    [Fact]
    public async Task Test_StreamRequest_HandlerThrowsException()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ExceptionThrowingStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new ExceptionThrowingStreamRequest { ThrowAfterCount = 2, ExceptionMessage = "Stream error" };
        var results = new List<int>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
            {
                results.Add(item);
            }
        });

        Assert.Equal("Stream error", exception.Message);
        Assert.Equal(2, results.Count);
        Assert.Equal([1, 2], results);
    }

    [Fact]
    public async Task Test_LargeDataRequest()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(LargeDataRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        byte[] largeData = new byte[1024 * 1024];
        new Random().NextBytes(largeData);

        var request = new LargeDataRequest { Data = largeData };
        int response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Equal(largeData.Length, response);
    }

    [Fact]
    public async Task Test_ConcurrentRequests_SameType()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(ConcurrentTestRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Singleton);

        var mediator = serviceContainer.GetService<IMediator>();

        var tasks = new List<Task<string>>();

        for (int i = 0; i < 100; i++)
        {
            var request = new ConcurrentTestRequest { Id = i, Data = $"Data{i}" };
            tasks.Add(mediator.SendAsync(request, CancellationToken.None).AsTask());
        }

        string[] results = await Task.WhenAll(tasks);

        Assert.Equal(100, results.Length);
        Assert.All(results, result => Assert.StartsWith("Processed:", result));

        string[] uniqueResults = results.Distinct().ToArray();
        Assert.Equal(100, uniqueResults.Length);
    }

    [Fact]
    public async Task Test_StreamRequest_Empty_WithPipeline()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(LoggingStreamBehavior).Assembly];
            options.StreamPipelineBehaviorTypes = [typeof(LoggingStreamBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 0, StartValue = 1 };
        var results = new List<int>();

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        Assert.Empty(results);

        var executionOrder = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(LoggingStreamBehavior), executionOrder[0]);
    }

    [Fact]
    public async Task Test_DefaultValueRequests()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(DefaultValueRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new DefaultValueRequest();
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Contains("Default", response);
        Assert.Contains("0", response);
        Assert.Contains("False", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test_MemoryPressure_LargeStreamRequest()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(NumberStreamRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new NumberStreamRequest { Count = 1000, StartValue = 1 };
        int processedCount = 0;
        long sumTotal = 0L;

        await foreach (int item in mediator.CreateStreamAsync(request, CancellationToken.None))
        {
            sumTotal += item;
            processedCount++;

            if (processedCount >= 100)
                break;
        }

        Assert.True(processedCount >= 100);
        Assert.True(sumTotal > 0);
        Assert.Equal(5050, sumTotal);

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public async Task Test_Unicode_And_SpecialCharacters()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(UnicodeRequest).Assembly];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        string specialChars = "?? Hello ??! ?o?l ?? \t\n\r \"'\\";
        var request = new UnicodeRequest { Text = specialChars };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Contains(specialChars, response);
        Assert.Contains("??", response);
        Assert.Contains("??", response);
        Assert.Contains("?o?l", response);
    }

    [Fact]
    public async Task Test_PipelineBehavior_ModifiesRequestObject()
    {
        var serviceContainer = new ServiceContainer();

        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(RequestModifyingBehavior).Assembly];
            options.PipelineBehaviorTypes = [typeof(RequestModifyingBehavior)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetService<IMediator>();

        var request = new MutableRequest { Value = 10, Text = "Original" };
        string response = await mediator.SendAsync(request, CancellationToken.None);

        Assert.Contains("Modified", response);
        Assert.Contains("100", response);

        var executionOrder = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executionOrder);
        Assert.Equal(nameof(RequestModifyingBehavior), executionOrder[0]);
    }
}
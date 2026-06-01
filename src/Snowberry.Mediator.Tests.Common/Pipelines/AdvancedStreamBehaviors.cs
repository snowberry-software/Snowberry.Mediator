using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

// Chained transformation behaviors
[PipelineOverwritePriority(Priority = 300)]
public class ChainedStreamBehavior1 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior1));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 10; // Multiply by 10
        }
    }
}

[PipelineOverwritePriority(Priority = 200)]
public class ChainedStreamBehavior2 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior2));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 100; // Add 100
        }
    }
}

[PipelineOverwritePriority(Priority = 100)]
public class ChainedStreamBehavior3 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior3));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 2; // Multiply by 2
        }
    }
}

public class ChainedStreamBehavior4 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior4));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 1; // Add 1
        }
    }
}

// Backpressure simulation behavior
public class BackpressureBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(NumberStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<NumberStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(BackpressureBehavior));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Simulate processing delay (backpressure)
            await Task.Delay(1, cancellationToken);
            yield return item;
        }
    }
}

// Conditional filtering behavior
public class ConditionalFilterBehavior : IStreamPipelineBehavior<FilterableStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(FilterableStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<FilterableStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ConditionalFilterBehavior));
        await foreach (int item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            if (request.FilterCondition(item))
            {
                yield return item;
            }
        }
    }
}

// Exception recovery behavior that does not delegate to next; it synthesizes its own stream.
public class ExceptionRecoveryBehavior : IStreamPipelineBehavior<FaultyStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(FaultyStreamRequest request, TNext next, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<FaultyStreamRequest, int>
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ExceptionRecoveryBehavior));

        // Synthesize the expected stream directly because the underlying handler can't be resumed after it throws.
        for (int i = 1; i <= request.Count; i++)
        {
            if (request.FaultAtPositions.Contains(i))
            {
                yield return -1;
            }
            else
            {
                yield return i;
            }

            if (cancellationToken.IsCancellationRequested)
                yield break;
        }

        await Task.CompletedTask;
    }
}
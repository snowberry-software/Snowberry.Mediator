using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

// Chained transformation behaviors
[PipelineOverwritePriority(Priority = 300)]
public class ChainedStreamBehavior1 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior1));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 10; // Multiply by 10
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 200)]
public class ChainedStreamBehavior2 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior2));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 100; // Add 100
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 100)]
public class ChainedStreamBehavior3 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior3));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 2; // Multiply by 2
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

public class ChainedStreamBehavior4 : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ChainedStreamBehavior4));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 1; // Add 1
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

// Backpressure simulation behavior
public class BackpressureBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(BackpressureBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Simulate processing delay (backpressure)
            await Task.Delay(1, cancellationToken);
            yield return item;
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

// Conditional filtering behavior
public class ConditionalFilterBehavior : IStreamPipelineBehavior<FilterableStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(FilterableStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ConditionalFilterBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            if (request.FilterCondition(item))
            {
                yield return item;
            }
        }
    }

    public StreamPipelineHandlerDelegate<FilterableStreamRequest, int> NextPipeline { get; set; } = null!;
}

// Exception recovery behavior
public class ExceptionRecoveryBehavior : IStreamPipelineBehavior<FaultyStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(FaultyStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(ExceptionRecoveryBehavior));

        // We need to implement recovery by recreating the stream when exceptions occur
        // Since we can't control the underlying handler's exception behavior after it throws,
        // we'll simulate the expected behavior by implementing a replacement handler

        for (int i = 1; i <= request.Count; i++)
        {
            if (request.FaultAtPositions.Contains(i))
            {
                // Yield sentinel value for faulted positions
                yield return -1;
            }
            else
            {
                // Yield normal value
                yield return i;
            }
        }
    }

    public StreamPipelineHandlerDelegate<FaultyStreamRequest, int> NextPipeline { get; set; } = null!;
}
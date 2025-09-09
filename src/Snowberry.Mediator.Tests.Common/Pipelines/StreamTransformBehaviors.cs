using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

[PipelineOverwritePriority(Priority = 100)]
public class FilterStreamBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(FilterStreamBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Only yield even numbers
            if (item % 2 == 0)
            {
                yield return item;
            }
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 50)]
public class MultiplyStreamBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(MultiplyStreamBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 2;
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

[PipelineOverwritePriority(Priority = 10)]
public class AddStreamBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(AddStreamBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 5;
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}

public class LoggingStreamBehavior : IStreamPipelineBehavior<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(LoggingStreamBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            // Log the item (in real scenario this would log to a logger)
            Console.WriteLine($"Stream item: {item}");
            yield return item; // Pass through unchanged
        }
    }

    public StreamPipelineHandlerDelegate<NumberStreamRequest, int> NextPipeline { get; set; } = null!;
}
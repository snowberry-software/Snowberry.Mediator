using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

/// <summary>
/// High priority concrete pipeline behavior for mixed testing
/// </summary>
[PipelineOverwritePriority(Priority = 200)]
public class HighPriorityConcretePipelineBehavior : IPipelineBehavior<MixedPipelineTestRequest, string>
{
    public async ValueTask<string> HandleAsync(MixedPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(HighPriorityConcretePipelineBehavior));
        string response = await NextPipeline(request, cancellationToken);
        return $"[H:{response}]"; // High priority wrapper
    }

    public PipelineHandlerDelegate<MixedPipelineTestRequest, string> NextPipeline { get; set; } = null!;
}

/// <summary>
/// Low priority concrete pipeline behavior for mixed testing
/// </summary>
[PipelineOverwritePriority(Priority = 25)]
public class LowPriorityConcretePipelineBehavior : IPipelineBehavior<MixedPipelineTestRequest, string>
{
    public async ValueTask<string> HandleAsync(MixedPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(LowPriorityConcretePipelineBehavior));
        string response = await NextPipeline(request, cancellationToken);
        return $"[L:{response}]"; // Low priority wrapper
    }

    public PipelineHandlerDelegate<MixedPipelineTestRequest, string> NextPipeline { get; set; } = null!;
}

/// <summary>
/// High priority concrete stream pipeline behavior for mixed testing
/// </summary>
[PipelineOverwritePriority(Priority = 150)]
public class HighPriorityConcreteStreamPipelineBehavior : IStreamPipelineBehavior<MixedStreamPipelineTestRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(MixedStreamPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(HighPriorityConcreteStreamPipelineBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item + 1000; // High priority transformation
        }
    }

    public StreamPipelineHandlerDelegate<MixedStreamPipelineTestRequest, int> NextPipeline { get; set; } = null!;
}

/// <summary>
/// Low priority concrete stream pipeline behavior for mixed testing
/// </summary>
[PipelineOverwritePriority(Priority = 30)]
public class LowPriorityConcreteStreamPipelineBehavior : IStreamPipelineBehavior<MixedStreamPipelineTestRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(MixedStreamPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution(nameof(LowPriorityConcreteStreamPipelineBehavior));
        await foreach (int item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item * 2; // Low priority transformation
        }
    }

    public StreamPipelineHandlerDelegate<MixedStreamPipelineTestRequest, int> NextPipeline { get; set; } = null!;
}
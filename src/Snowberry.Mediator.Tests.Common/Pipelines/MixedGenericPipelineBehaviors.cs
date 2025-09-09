using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

/// <summary>
/// High priority open generic pipeline behavior for normal requests
/// </summary>
[PipelineOverwritePriority(Priority = 150)]
public class HighPriorityGenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution($"HighPriorityGenericPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        var response = await NextPipeline(request, cancellationToken);
        return response;
    }

    public PipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}

/// <summary>
/// Medium priority open generic pipeline behavior for normal requests
/// </summary>
[PipelineOverwritePriority(Priority = 75)]
public class MediumPriorityGenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution($"MediumPriorityGenericPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        var response = await NextPipeline(request, cancellationToken);
        return response;
    }

    public PipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}

/// <summary>
/// High priority open generic stream pipeline behavior
/// </summary>
[PipelineOverwritePriority(Priority = 120)]
public class HighPriorityGenericStreamPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution($"HighPriorityGenericStreamPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        await foreach (var item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public StreamPipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}

/// <summary>
/// Medium priority open generic stream pipeline behavior
/// </summary>
[PipelineOverwritePriority(Priority = 80)]
public class MediumPriorityGenericStreamPipelineBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        StreamPipelineExecutionTracker.RecordExecution($"MediumPriorityGenericStreamPipelineBehavior<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
        await foreach (var item in NextPipeline(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public StreamPipelineHandlerDelegate<TRequest, TResponse> NextPipeline { get; set; } = null!;
}
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class CancellationCheckingPipelineBehavior : IPipelineBehavior<DelayedRequest, string>
{
    public async ValueTask<string> HandleAsync(DelayedRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(CancellationCheckingPipelineBehavior));

        // Check cancellation token multiple times during execution
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(10, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        string response = await NextPipeline(request, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return response;
    }

    public PipelineHandlerDelegate<DelayedRequest, string> NextPipeline { get; set; } = null!;
}
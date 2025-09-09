using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class ComplexRequestPipelineBehavior : IPipelineBehavior<ComplexRequest, string>
{
    public async ValueTask<string> HandleAsync(ComplexRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(ComplexRequestPipelineBehavior));
        string response = await NextPipeline(request, cancellationToken);
        return $"[{response}]";
    }

    public PipelineHandlerDelegate<ComplexRequest, string> NextPipeline { get; set; } = null!;
}
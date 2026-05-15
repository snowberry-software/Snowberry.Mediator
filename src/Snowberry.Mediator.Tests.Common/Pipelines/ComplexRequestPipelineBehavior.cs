using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class ComplexRequestPipelineBehavior : IPipelineBehavior<ComplexRequest, string>
{
    public async ValueTask<string> HandleAsync<TNext>(ComplexRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<ComplexRequest, string>
    {
        PipelineExecutionTracker.RecordExecution(nameof(ComplexRequestPipelineBehavior));
        string response = await next.InvokeAsync(request, cancellationToken);
        return $"[{response}]";
    }
}
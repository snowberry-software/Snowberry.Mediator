using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class ExceptionHandlingBehavior : IPipelineBehavior<ExceptionThrowingRequest, string>
{
    public async ValueTask<string> HandleAsync(ExceptionThrowingRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(ExceptionHandlingBehavior));

        try
        {
            return await NextPipeline(request, cancellationToken);
        }
        catch (CustomBusinessException ex)
        {
            // Transform exception into successful response
            return $"Exception caught: {ex.Message}";
        }
    }

    public PipelineHandlerDelegate<ExceptionThrowingRequest, string> NextPipeline { get; set; } = null!;
}

public class RequestModifyingBehavior : IPipelineBehavior<MutableRequest, string>
{
    public async ValueTask<string> HandleAsync(MutableRequest request, CancellationToken cancellationToken = default)
    {
        PipelineExecutionTracker.RecordExecution(nameof(RequestModifyingBehavior));

        // Modify the request before passing to handler
        request.Value *= 10;
        request.Text = $"Modified {request.Text}";

        return await NextPipeline(request, cancellationToken);
    }

    public PipelineHandlerDelegate<MutableRequest, string> NextPipeline { get; set; } = null!;
}
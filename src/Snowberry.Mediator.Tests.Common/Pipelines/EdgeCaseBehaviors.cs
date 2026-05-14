using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Pipelines;

public class ExceptionHandlingBehavior : IPipelineBehavior<ExceptionThrowingRequest, string>
{
    public async ValueTask<string> HandleAsync<TNext>(ExceptionThrowingRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<ExceptionThrowingRequest, string>
    {
        PipelineExecutionTracker.RecordExecution(nameof(ExceptionHandlingBehavior));

        try
        {
            return await next.InvokeAsync(request, cancellationToken);
        }
        catch (CustomBusinessException ex)
        {
            // Transform exception into successful response
            return $"Exception caught: {ex.Message}";
        }
    }
}

public class RequestModifyingBehavior : IPipelineBehavior<MutableRequest, string>
{
    public async ValueTask<string> HandleAsync<TNext>(MutableRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<MutableRequest, string>
    {
        PipelineExecutionTracker.RecordExecution(nameof(RequestModifyingBehavior));

        // Modify the request before passing to handler
        request.Value *= 10;
        request.Text = $"Modified {request.Text}";

        return await next.InvokeAsync(request, cancellationToken);
    }
}
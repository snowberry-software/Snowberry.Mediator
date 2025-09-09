using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class CancellationThrowingRequestHandler : IRequestHandler<CancellationThrowingRequest, string>
{
    public async ValueTask<string> HandleAsync(CancellationThrowingRequest request, CancellationToken cancellationToken = default)
    {
        // Simulate some work before checking cancellation
        await Task.Delay(10, CancellationToken.None);

        // Throw TaskCanceledException instead of checking cancellation token
        if (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("Request was cancelled");
        }

        return $"Processed: {request.Message}";
    }
}
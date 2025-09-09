using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class DelayedRequestHandler : IRequestHandler<DelayedRequest, string>
{
    public async ValueTask<string> HandleAsync(DelayedRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return $"Delayed:{request.Message}";
    }
}
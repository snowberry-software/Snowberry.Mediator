using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class CounterRequestHandler : IRequestHandler<CounterRequest, int>
{
    /// <inheritdoc/>
    public ValueTask<int> HandleAsync(CounterRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(CounterRequest.c_InitialValue);
    }
}

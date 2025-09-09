using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class PriorityTestRequestHandler : IRequestHandler<PriorityTestRequest, string>
{
    public ValueTask<string> HandleAsync(PriorityTestRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"Handled: {request.Message}");
    }
}

public class MultiBehaviorRequestHandler : IRequestHandler<MultiBehaviorRequest, int>
{
    public ValueTask<int> HandleAsync(MultiBehaviorRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(request.Value);
    }
}

public class PerformanceTestRequestHandler : IRequestHandler<PerformanceTestRequest, int>
{
    public ValueTask<int> HandleAsync(PerformanceTestRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(request.BaseValue);
    }
}

public class InheritanceTestRequestHandler : IRequestHandler<InheritanceTestRequest, string>
{
    public ValueTask<string> HandleAsync(InheritanceTestRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"Handled: {request.Data}");
    }
}
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class ComplexRequestHandler : IRequestHandler<ComplexRequest, string>
{
    public ValueTask<string> HandleAsync(ComplexRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"{request.Message} x{request.Factor}");
    }
}
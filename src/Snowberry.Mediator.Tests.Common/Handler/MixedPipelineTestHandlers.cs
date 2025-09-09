using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

/// <summary>
/// Handler for mixed pipeline test request
/// </summary>
public class MixedPipelineTestRequestHandler : IRequestHandler<MixedPipelineTestRequest, string>
{
    public ValueTask<string> HandleAsync(MixedPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"Handled:{request.Message}:{request.Value}");
    }
}

/// <summary>
/// Handler for mixed stream pipeline test request
/// </summary>
public class MixedStreamPipelineTestRequestHandler : IStreamRequestHandler<MixedStreamPipelineTestRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(MixedStreamPipelineTestRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.Count; i++)
        {
            yield return request.StartValue + i;
        }
    }
}
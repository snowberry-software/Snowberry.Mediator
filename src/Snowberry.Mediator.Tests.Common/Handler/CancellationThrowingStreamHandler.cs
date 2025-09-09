using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class CancellationThrowingStreamHandler : IStreamRequestHandler<CancellationThrowingStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(CancellationThrowingStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= request.ThrowAfterCount; i++)
        {
            await Task.Delay(5, CancellationToken.None);
            yield return i;
        }

        // Throw TaskCanceledException after yielding the specified count
        throw new TaskCanceledException("Stream was cancelled after specified count");
    }
}
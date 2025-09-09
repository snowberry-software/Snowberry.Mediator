using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class NumberStreamRequestHandler : IStreamRequestHandler<NumberStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(NumberStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken); // Small delay to make it truly async
            yield return request.StartValue + i;
        }
    }
}
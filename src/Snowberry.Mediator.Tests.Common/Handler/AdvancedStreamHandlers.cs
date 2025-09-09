using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class ComplexDataStreamHandler : IStreamRequestHandler<ComplexDataStreamRequest, ComplexDataItem>
{
    public async IAsyncEnumerable<ComplexDataItem> HandleAsync(ComplexDataStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);

            yield return new ComplexDataItem
            {
                Name = $"{request.Prefix}{i + 1}",
                Date = request.StartDate.AddDays(i),
                Sequence = i + 1
            };
        }
    }
}

public class DisposableStreamHandler : IStreamRequestHandler<DisposableStreamRequest, DisposableResource>
{
    public async IAsyncEnumerable<DisposableResource> HandleAsync(DisposableStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.ResourceCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);

            yield return new DisposableResource { Name = $"Resource{i + 1}" };
        }
    }
}

public class FilterableStreamHandler : IStreamRequestHandler<FilterableStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(FilterableStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);

            yield return request.StartValue + i;
        }
    }
}

public class FaultyStreamHandler : IStreamRequestHandler<FaultyStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(FaultyStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);

            // Throw exception at specified positions
            if (request.FaultAtPositions.Contains(i + 1))
            {
                throw new InvalidOperationException($"Fault at position {i + 1}");
            }

            yield return i + 1;
        }
    }
}
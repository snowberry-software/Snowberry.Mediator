using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.Common.Handler;

public class NullableRequestHandler : IRequestHandler<NullableRequest, string>
{
    public ValueTask<string> HandleAsync(NullableRequest request, CancellationToken cancellationToken = default)
    {
        string nullableValue = request.NullableString ?? "null";
        return ValueTask.FromResult($"Nullable: {nullableValue}, Required: {request.RequiredString}");
    }
}

public class ExceptionThrowingRequestHandler : IRequestHandler<ExceptionThrowingRequest, string>
{
    public ValueTask<string> HandleAsync(ExceptionThrowingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ShouldThrow)
        {
            throw new CustomBusinessException(request.Message);
        }

        return ValueTask.FromResult($"No exception: {request.Message}");
    }
}

public class ExceptionThrowingStreamHandler : IStreamRequestHandler<ExceptionThrowingStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(ExceptionThrowingStreamRequest request, CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= request.ThrowAfterCount; i++)
        {
            await Task.Delay(1, cancellationToken);
            yield return i;
        }

        // Throw exception after yielding the specified count
        throw new InvalidOperationException(request.ExceptionMessage);
    }
}

public class LargeDataRequestHandler : IRequestHandler<LargeDataRequest, int>
{
    public ValueTask<int> HandleAsync(LargeDataRequest request, CancellationToken cancellationToken = default)
    {
        // Process the large data (just return length for testing)
        return ValueTask.FromResult(request.Data.Length);
    }
}

public class ConcurrentTestRequestHandler : IRequestHandler<ConcurrentTestRequest, string>
{
    private static readonly object _lock = new();
    private static int _processingCounter = 0;

    public async ValueTask<string> HandleAsync(ConcurrentTestRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _processingCounter++;
        }

        // Simulate some processing time
        await Task.Delay(Random.Shared.Next(1, 10), cancellationToken);

        return $"Processed: Id={request.Id}, Data={request.Data}, Counter={_processingCounter}";
    }
}

public class DefaultValueRequestHandler : IRequestHandler<DefaultValueRequest, string>
{
    public ValueTask<string> HandleAsync(DefaultValueRequest request, CancellationToken cancellationToken = default)
    {
        string dateStr = request.OptionalDate?.ToString("yyyy-MM-dd") ?? "null";
        return ValueTask.FromResult($"Text: {request.Text}, Number: {request.Number}, Flag: {request.Flag}, Date: {dateStr}");
    }
}

public class UnicodeRequestHandler : IRequestHandler<UnicodeRequest, string>
{
    public ValueTask<string> HandleAsync(UnicodeRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"Unicode processed: {request.Text} (Length: {request.Text.Length})");
    }
}

public class MutableRequestHandler : IRequestHandler<MutableRequest, string>
{
    public ValueTask<string> HandleAsync(MutableRequest request, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult($"Processed: {request.Text}, Value: {request.Value}");
    }
}
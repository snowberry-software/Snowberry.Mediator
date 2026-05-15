using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Attributes;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

// Local test types kept close to the OTel tests that use them.

public class ThrowingRequest : IRequest<ThrowingRequest, int> { }

public class ThrowingRequestHandler : IRequestHandler<ThrowingRequest, int>
{
    public ValueTask<int> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("boom");
}

public class NestingRequest : IRequest<NestingRequest, int> { }

public class NestingRequestHandler : IRequestHandler<NestingRequest, int>
{
    private readonly IMediator _mediator;
    public NestingRequestHandler(IMediator mediator) => _mediator = mediator;

    public async ValueTask<int> HandleAsync(NestingRequest request, CancellationToken cancellationToken = default)
        => await _mediator.SendAsync(new CounterRequest(), cancellationToken);
}

public class ThrowingStreamRequest : IStreamRequest<ThrowingStreamRequest, int> { }

public class ThrowingStreamRequestHandler : IStreamRequestHandler<ThrowingStreamRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(ThrowingStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return 1;
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("stream boom");
    }
}

[PipelineOverwritePriority(Priority = 10)]
public class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

[PipelineOverwritePriority(Priority = 5)]
public class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

[PipelineOverwritePriority(Priority = 1)]
public class ThirdBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

public class SecondNotification : INotification
{
    public string Message { get; set; } = string.Empty;
}

public class SecondNotificationHandler : INotificationHandler<SecondNotification>
{
    public ValueTask HandleAsync(SecondNotification notification, CancellationToken cancellationToken = default)
        => default;
}

public class FakeMediator : IMediator
{
    public int SendInvocations;

    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(IRequest<TRequest, TResponse> request, CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        Interlocked.Increment(ref SendInvocations);
        return new ValueTask<TResponse>(default(TResponse)!);
    }

    public IAsyncEnumerable<TResponse> CreateStreamAsync<TRequest, TResponse>(IStreamRequest<TRequest, TResponse> request, CancellationToken cancellationToken = default)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
        => Empty<TResponse>();

    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => default;

    private static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}

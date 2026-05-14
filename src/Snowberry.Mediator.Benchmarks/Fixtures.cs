using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

namespace Snowberry.Mediator.Benchmarks;

// ---------- NoPipeline ----------

public sealed class NoPipelineRequest : IRequest<NoPipelineRequest, int>;

public sealed class NoPipelineRequestHandler : IRequestHandler<NoPipelineRequest, int>
{
    public ValueTask<int> HandleAsync(NoPipelineRequest request, CancellationToken cancellationToken = default)
        => new(42);
}

// ---------- Specific1 ----------

public sealed class Specific1Request : IRequest<Specific1Request, int>;

public sealed class Specific1RequestHandler : IRequestHandler<Specific1Request, int>
{
    public ValueTask<int> HandleAsync(Specific1Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public sealed class Specific1Behavior1 : IPipelineBehavior<Specific1Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Specific1Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific1Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

// Async (await Task.Yield) variant - for Phase 0.5 diagnosis (state-machine box source)
public sealed class Specific1AsyncRequest : IRequest<Specific1AsyncRequest, int>;

public sealed class Specific1AsyncRequestHandler : IRequestHandler<Specific1AsyncRequest, int>
{
    public async ValueTask<int> HandleAsync(Specific1AsyncRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return 42;
    }
}

public sealed class Specific1AsyncBehavior1 : IPipelineBehavior<Specific1AsyncRequest, int>
{
    public async ValueTask<int> HandleAsync<TNext>(Specific1AsyncRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific1AsyncRequest, int>
        => await next.InvokeAsync(request, cancellationToken);
}

// ---------- Specific3 ----------

public sealed class Specific3Request : IRequest<Specific3Request, int>;

public sealed class Specific3RequestHandler : IRequestHandler<Specific3Request, int>
{
    public ValueTask<int> HandleAsync(Specific3Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public sealed class Specific3Behavior1 : IPipelineBehavior<Specific3Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Specific3Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific3Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class Specific3Behavior2 : IPipelineBehavior<Specific3Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Specific3Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific3Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class Specific3Behavior3 : IPipelineBehavior<Specific3Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Specific3Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific3Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

// ---------- Specific10 ----------

public sealed class Specific10Request : IRequest<Specific10Request, int>;

public sealed class Specific10RequestHandler : IRequestHandler<Specific10Request, int>
{
    public ValueTask<int> HandleAsync(Specific10Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public abstract class Specific10BehaviorBase : IPipelineBehavior<Specific10Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Specific10Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Specific10Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class Specific10Behavior1 : Specific10BehaviorBase;
public sealed class Specific10Behavior2 : Specific10BehaviorBase;
public sealed class Specific10Behavior3 : Specific10BehaviorBase;
public sealed class Specific10Behavior4 : Specific10BehaviorBase;
public sealed class Specific10Behavior5 : Specific10BehaviorBase;
public sealed class Specific10Behavior6 : Specific10BehaviorBase;
public sealed class Specific10Behavior7 : Specific10BehaviorBase;
public sealed class Specific10Behavior8 : Specific10BehaviorBase;
public sealed class Specific10Behavior9 : Specific10BehaviorBase;
public sealed class Specific10Behavior10 : Specific10BehaviorBase;

// ---------- OpenGeneric1 ----------

public sealed class OpenGeneric1Request : IRequest<OpenGeneric1Request, int>;

public sealed class OpenGeneric1RequestHandler : IRequestHandler<OpenGeneric1Request, int>
{
    public ValueTask<int> HandleAsync(OpenGeneric1Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public sealed class OpenBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

// Async variant for diagnosis
public sealed class OpenGeneric1AsyncRequest : IRequest<OpenGeneric1AsyncRequest, int>;
public sealed class OpenGeneric1AsyncRequestHandler : IRequestHandler<OpenGeneric1AsyncRequest, int>
{
    public async ValueTask<int> HandleAsync(OpenGeneric1AsyncRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return 42;
    }
}

public sealed class OpenAsyncBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => await next.InvokeAsync(request, cancellationToken);
}

// ---------- OpenGeneric3 ----------

public sealed class OpenGeneric3Request : IRequest<OpenGeneric3Request, int>;

public sealed class OpenGeneric3RequestHandler : IRequestHandler<OpenGeneric3Request, int>
{
    public ValueTask<int> HandleAsync(OpenGeneric3Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public sealed class OpenBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class OpenBehavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

// ---------- Mixed4 (2 specific + 2 open-generic) ----------

public sealed class Mixed4Request : IRequest<Mixed4Request, int>;

public sealed class Mixed4RequestHandler : IRequestHandler<Mixed4Request, int>
{
    public ValueTask<int> HandleAsync(Mixed4Request request, CancellationToken cancellationToken = default)
        => new(42);
}

public sealed class Mixed4SpecificBehavior1 : IPipelineBehavior<Mixed4Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Mixed4Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Mixed4Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class Mixed4SpecificBehavior2 : IPipelineBehavior<Mixed4Request, int>
{
    public ValueTask<int> HandleAsync<TNext>(Mixed4Request request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<Mixed4Request, int>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class MixedOpenBehavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

public sealed class MixedOpenBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
        => next.InvokeAsync(request, cancellationToken);
}

// ---------- Notifications ----------

public sealed class SpecificNotification3 : INotification;
public sealed class OpenGenericNotification3 : INotification;
public sealed class MixedNotification3 : INotification;

public sealed class SpecificNotification3Handler1 : INotificationHandler<SpecificNotification3>
{
    public ValueTask HandleAsync(SpecificNotification3 notification, CancellationToken cancellationToken = default)
        => default;
}
public sealed class SpecificNotification3Handler2 : INotificationHandler<SpecificNotification3>
{
    public ValueTask HandleAsync(SpecificNotification3 notification, CancellationToken cancellationToken = default)
        => default;
}
public sealed class SpecificNotification3Handler3 : INotificationHandler<SpecificNotification3>
{
    public ValueTask HandleAsync(SpecificNotification3 notification, CancellationToken cancellationToken = default)
        => default;
}

public sealed class OpenNotificationHandler1<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
        => default;
}
public sealed class OpenNotificationHandler2<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
        => default;
}
public sealed class OpenNotificationHandler3<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
        => default;
}

public sealed class MixedNotification3Specific1 : INotificationHandler<MixedNotification3>
{
    public ValueTask HandleAsync(MixedNotification3 notification, CancellationToken cancellationToken = default)
        => default;
}
public sealed class MixedNotification3Specific2 : INotificationHandler<MixedNotification3>
{
    public ValueTask HandleAsync(MixedNotification3 notification, CancellationToken cancellationToken = default)
        => default;
}

public sealed class MixedOpenNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
        => default;
}

// ---------- Stream NoPipeline ----------

public sealed class StreamNoPipelineRequest : IStreamRequest<StreamNoPipelineRequest, int>;

public sealed class StreamNoPipelineRequestHandler : IStreamRequestHandler<StreamNoPipelineRequest, int>
{
    public async IAsyncEnumerable<int> HandleAsync(StreamNoPipelineRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
        await Task.CompletedTask;
    }
}

// ---------- Stream Specific1 ----------

public sealed class StreamSpecific1Request : IStreamRequest<StreamSpecific1Request, int>;

public sealed class StreamSpecific1RequestHandler : IStreamRequestHandler<StreamSpecific1Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync(StreamSpecific1Request request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
        await Task.CompletedTask;
    }
}

public sealed class StreamSpecific1Behavior1 : IStreamPipelineBehavior<StreamSpecific1Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(StreamSpecific1Request request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<StreamSpecific1Request, int>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

// ---------- Stream Specific3 ----------

public sealed class StreamSpecific3Request : IStreamRequest<StreamSpecific3Request, int>;

public sealed class StreamSpecific3RequestHandler : IStreamRequestHandler<StreamSpecific3Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync(StreamSpecific3Request request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
        await Task.CompletedTask;
    }
}

public abstract class StreamSpecific3BehaviorBase : IStreamPipelineBehavior<StreamSpecific3Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(StreamSpecific3Request request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<StreamSpecific3Request, int>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

public sealed class StreamSpecific3Behavior1 : StreamSpecific3BehaviorBase;
public sealed class StreamSpecific3Behavior2 : StreamSpecific3BehaviorBase;
public sealed class StreamSpecific3Behavior3 : StreamSpecific3BehaviorBase;

// ---------- Stream Specific10 ----------

public sealed class StreamSpecific10Request : IStreamRequest<StreamSpecific10Request, int>;

public sealed class StreamSpecific10RequestHandler : IStreamRequestHandler<StreamSpecific10Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync(StreamSpecific10Request request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
        await Task.CompletedTask;
    }
}

public abstract class StreamSpecific10BehaviorBase : IStreamPipelineBehavior<StreamSpecific10Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync<TNext>(StreamSpecific10Request request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<StreamSpecific10Request, int>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

public sealed class StreamSpecific10Behavior1 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior2 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior3 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior4 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior5 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior6 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior7 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior8 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior9 : StreamSpecific10BehaviorBase;
public sealed class StreamSpecific10Behavior10 : StreamSpecific10BehaviorBase;

// ---------- Stream OpenGeneric3 ----------

public sealed class StreamOpenGeneric3Request : IStreamRequest<StreamOpenGeneric3Request, int>;

public sealed class StreamOpenGeneric3RequestHandler : IStreamRequestHandler<StreamOpenGeneric3Request, int>
{
    public async IAsyncEnumerable<int> HandleAsync(StreamOpenGeneric3Request request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return i;
        }
        await Task.CompletedTask;
    }
}

public sealed class OpenStreamBehavior1<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync<TNext>(TRequest request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

public sealed class OpenStreamBehavior2<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync<TNext>(TRequest request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

public sealed class OpenStreamBehavior3<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    public async IAsyncEnumerable<TResponse> HandleAsync<TNext>(TRequest request, TNext next,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>
    {
        await foreach (var item in next.InvokeAsync(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
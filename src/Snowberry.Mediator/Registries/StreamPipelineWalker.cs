using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Stack-resident stream pipeline walker. Implements
/// <see cref="IStreamPipelineContinuation{TRequest,TResponse}"/> as a <see langword="readonly struct"/>.
/// Each <see cref="InvokeAsync"/> call advances to the next step by constructing a fresh walker with the
/// incremented index and passing it by value to the next behavior.
/// </summary>
/// <remarks>
/// When the consuming behavior is constrained <c>where TNext : struct, IStreamPipelineContinuation&lt;,&gt;</c>,
/// the JIT specializes the behavior method per walker type and devirtualizes <see cref="InvokeAsync"/> to a
/// direct call - no delegate, no boxing of the walker, and no per-link chain allocations.
/// </remarks>
internal readonly struct StreamPipelineWalker<TRequest, TResponse> : IStreamPipelineContinuation<TRequest, TResponse>
    where TRequest : class, IStreamRequest<TRequest, TResponse>
{
    private readonly int _index;
    private readonly IServiceProvider _sp;
    private readonly IStreamRequestHandler<TRequest, TResponse> _terminal;
    private readonly Type[] _types;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StreamPipelineWalker(IServiceProvider sp, IStreamRequestHandler<TRequest, TResponse> terminal, Type[] types, int index)
    {
        _sp = sp;
        _terminal = terminal;
        _types = types;
        _index = index;
    }

    private static async IAsyncEnumerable<TResponse> InvokeInstrumentedAsync(
        IStreamPipelineBehavior<TRequest, TResponse> behavior,
        Type behaviorType,
        TRequest request,
        StreamPipelineWalker<TRequest, TResponse> next,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var activity = MediatorDiagnostics.s_PipelineSource.StartActivity(
            "Mediator.Behavior " + behaviorType.Name, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("snowberry.mediator.behavior.type", behaviorType.Name);
            activity.SetTag("snowberry.mediator.request.type", typeof(TRequest).Name);
        }
        string status = "failure";
        try
        {
            await foreach (var item in behavior.HandleAsync(request, next, ct).ConfigureAwait(false))
                yield return item;
            status = "success";
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        finally
        {
            if (status != "success") activity?.SetStatus(ActivityStatusCode.Error);
        }
    }

    [SuppressMessage("Trimming", "IL2026", Justification = "Stream pipeline behavior types are explicitly registered.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (_index >= _types.Length)
            return _terminal.HandleAsync(request, cancellationToken);

        var behaviorType = _types[_index];
        var behavior = Unsafe.As<IStreamPipelineBehavior<TRequest, TResponse>>(_sp.GetService(behaviorType))!;
        var next = new StreamPipelineWalker<TRequest, TResponse>(_sp, _terminal, _types, _index + 1);

        if (!MediatorDiagnostics.IsPipelineEnabled)
            return behavior.HandleAsync(request, next, cancellationToken);

        return InvokeInstrumentedAsync(behavior, behaviorType, request, next, cancellationToken);
    }
}
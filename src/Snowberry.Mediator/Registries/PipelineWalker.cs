using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Stack-resident pipeline walker. Implements <see cref="IPipelineContinuation{TRequest,TResponse}"/> as a
/// <see langword="readonly struct"/>. Each <see cref="InvokeAsync"/> call advances to the next step by
/// constructing a fresh walker with the incremented index and passing it by value to the next behavior.
/// </summary>
/// <remarks>
/// When the consuming behavior is constrained <c>where TNext : struct, IPipelineContinuation&lt;,&gt;</c>,
/// the JIT specializes the behavior method per walker type and devirtualizes <see cref="InvokeAsync"/> to a
/// direct call - no delegate, no boxing, zero allocation on synchronous fast paths.
/// </remarks>
internal readonly struct PipelineWalker<TRequest, TResponse> : IPipelineContinuation<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    private readonly int _index;
    private readonly IServiceProvider _sp;
    private readonly IRequestHandler<TRequest, TResponse> _terminal;
    private readonly Type[] _types;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PipelineWalker(IServiceProvider sp, IRequestHandler<TRequest, TResponse> terminal, Type[] types, int index)
    {
        _sp = sp;
        _terminal = terminal;
        _types = types;
        _index = index;
    }

    private static async ValueTask<TResponse> InvokeInstrumentedAsync(
        IPipelineBehavior<TRequest, TResponse> behavior,
        Type behaviorType,
        TRequest request,
        PipelineWalker<TRequest, TResponse> next,
        CancellationToken ct)
    {
        using var activity = MediatorDiagnostics.s_PipelineSource.StartActivity(
            "Mediator.Behavior " + behaviorType.Name, ActivityKind.Internal);
        if (activity is not null)
        {
            activity.SetTag("snowberry.mediator.behavior.type", behaviorType.Name);
            activity.SetTag("snowberry.mediator.request.type", typeof(TRequest).Name);
        }
        try
        {
            var result = await behavior.HandleAsync(request, next, ct).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    [SuppressMessage("Trimming", "IL2026", Justification = "Pipeline behavior types are explicitly registered.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (_index >= _types.Length)
            return _terminal.HandleAsync(request, cancellationToken);

        var behaviorType = _types[_index];
        var behavior = Unsafe.As<IPipelineBehavior<TRequest, TResponse>>(_sp.GetService(behaviorType))!;
        var next = new PipelineWalker<TRequest, TResponse>(_sp, _terminal, _types, _index + 1);

        if (!MediatorDiagnostics.IsPipelineEnabled)
            return behavior.HandleAsync(request, next, cancellationToken);

        return InvokeInstrumentedAsync(behavior, behaviorType, request, next, cancellationToken);
    }
}
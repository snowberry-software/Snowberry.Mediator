using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Default <see cref="IGlobalPipelineRegistry"/> implementation. Tracks
/// <see cref="Abstractions.Pipeline.IPipelineBehavior{TRequest, TResponse}"/> registrations and dispatches
/// requests through them in priority order, resolving each behavior from the supplied
/// <see cref="IServiceProvider"/> on every Send.
/// </summary>
public sealed class GlobalPipelineRegistry : BaseGlobalPipelineRegistry<PipelineBehaviorHandlerInfo>, IGlobalPipelineRegistry
{
    // Per-(TRequest,TResponse) cache of closed behavior types in execution order (index 0 = highest priority).
    // Per-instance so different registries - common in test suites - cannot conflict on shared request types.
    // Lookups are lock-free; misses build then TryAdd race-tolerantly.
    private readonly ConcurrentDictionary<(Type Request, Type Response), Type[]> _typeCache = new();

    /// <summary>Initializes a registry that closes open-generic behaviors via reflection.</summary>
    public GlobalPipelineRegistry()
    {
    }

    /// <summary>Initializes a registry that closes open-generic behaviors via a generated resolver.</summary>
    /// <param name="closedTypeResolver">Maps <c>(openHandlerType, requestType, responseType)</c> to the closed handler type.</param>
    public GlobalPipelineRegistry(Func<Type, Type, Type, Type>? closedTypeResolver) : base(closedTypeResolver)
    {
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingJustifications.PipelineBehaviors)]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = TrimmingJustifications.PipelineBehaviors)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = TrimmingJustifications.PipelineBehaviors)]
    public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(IServiceProvider serviceProvider, IRequestHandler<TRequest, TResponse> handler, TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        if (IsEmpty)
            return handler.HandleAsync(request, cancellationToken);

        // Fast path - single static-generic acquire-fence read of the cached entry.
        var entry = Volatile.Read(ref PipelineFastCache<TRequest, TResponse>.s_Current);
        if (entry is not null
            && ReferenceEquals(entry.Owner, this)
            && entry.Generation == Generation)
        {
            var types = entry.Types;
            if (types.Length == 0)
                return handler.HandleAsync(request, cancellationToken);
            return new PipelineWalker<TRequest, TResponse>(serviceProvider, handler, types, 0)
                .InvokeAsync(request, cancellationToken);
        }

        return ExecuteSlow(serviceProvider, handler, request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnBuilt()
    {
        // Frozen state changed - invalidate the per-pair closed-type cache so the next dispatch rebuilds it.
        _typeCache.Clear();
    }

    private ValueTask<TResponse> ExecuteSlow<TRequest, TResponse>(IServiceProvider serviceProvider, IRequestHandler<TRequest, TResponse> handler, TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        EnsureBuilt();

        var key = (typeof(TRequest), typeof(TResponse));
        if (!_typeCache.TryGetValue(key, out var types))
        {
            types = BuildBehaviorTypesFor(typeof(TRequest), typeof(TResponse));
            _typeCache.TryAdd(key, types);
        }

        // One allocation per slow-path miss. Release-fence via Volatile.Write makes the prior readonly-field
        // writes inside the constructor visible to any future Volatile.Read on the fast path.
        Volatile.Write(
            ref PipelineFastCache<TRequest, TResponse>.s_Current,
            new FastCacheEntry(this, Generation, types));

        if (types.Length == 0)
            return handler.HandleAsync(request, cancellationToken);

        return new PipelineWalker<TRequest, TResponse>(serviceProvider, handler, types, 0)
            .InvokeAsync(request, cancellationToken);
    }
}

/// <summary>
/// Per-<c>(TRequest, TResponse)</c> static cache holding the closed behavior-type array for that pair.
/// The single <see cref="s_Current"/> field is published via <c>Volatile.Write</c> and acquired via
/// <c>Volatile.Read</c>; readers reconcile against the registry's
/// <see cref="BaseGlobalPipelineRegistry{T}.Generation"/> to detect rebuilds.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal static class PipelineFastCache<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <summary>
    /// The most recently published cache entry, or <see langword="null"/> if no entry exists yet for
    /// this <c>(TRequest, TResponse)</c> pair.
    /// </summary>
    internal static FastCacheEntry? s_Current;
}

/// <summary>
/// Immutable carrier for a <see cref="PipelineFastCache{TRequest, TResponse}"/> entry, tying a sorted
/// behavior-type array to the registry instance and build generation that produced it.
/// </summary>
internal sealed class FastCacheEntry
{
    /// <summary>The <see cref="BaseGlobalPipelineRegistry{T}.Generation"/> value at the time the entry was built.</summary>
    public readonly int Generation;

    /// <summary>The registry instance that produced this entry.</summary>
    public readonly GlobalPipelineRegistry Owner;

    /// <summary>The closed behavior types in dispatch order (highest priority first).</summary>
    public readonly Type[] Types;

    /// <summary>
    /// Initializes a new <see cref="FastCacheEntry"/>.
    /// </summary>
    /// <param name="owner">The registry instance that produced this entry.</param>
    /// <param name="generation">The build generation when the entry was produced.</param>
    /// <param name="types">The closed behavior types in dispatch order.</param>
    public FastCacheEntry(GlobalPipelineRegistry owner, int generation, Type[] types)
    {
        Owner = owner;
        Generation = generation;
        Types = types;
    }
}
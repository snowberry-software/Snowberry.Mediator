#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Base implementation of <see cref="IBaseGlobalPipelineRegistry{T}"/> shared by the request and stream
/// pipeline registries. Tracks registered pipeline behaviors, exposes a read-optimized frozen snapshot to
/// derived dispatch code via <see cref="FrozenOpenGenericHandlers"/> and <see cref="TryGetFrozenSpecific"/>,
/// and signals snapshot changes via <see cref="OnBuilt"/>.
/// </summary>
/// <typeparam name="T">The concrete pipeline-behavior handler-info type tracked by the registry.</typeparam>
public class BaseGlobalPipelineRegistry<T> : IBaseGlobalPipelineRegistry<T>
    where T : PipelineBehaviorHandlerInfo
{
#if NET9_0_OR_GREATER
    protected readonly Lock _lock = new();
#else
    protected readonly object _lock = new();
#endif
    // Mutable registration state (writes under _lock).
    protected Dictionary<Type, List<PipelineBehaviorValue<T>>> _pipelineBehaviors = [];
    protected List<PipelineBehaviorValue<T>> _openGenericHandlers = [];
    protected int _registrationIndex = 0;

    // Read-optimized frozen snapshots (atomic reference writes via Build).
    // After Build: arrays are sorted by SortIndex descending so iteration order matches dispatch order.
#if NET8_0_OR_GREATER
    private FrozenDictionary<Type, PipelineBehaviorValue<T>[]> _frozenPipelineBehaviors
        = FrozenDictionary<Type, PipelineBehaviorValue<T>[]>.Empty;
#else
    private Dictionary<Type, PipelineBehaviorValue<T>[]> _frozenPipelineBehaviors = [];
#endif
    private PipelineBehaviorValue<T>[] _frozenOpenGenericHandlers = [];

    // Lock-free IsEmpty read (volatile field; set under _lock when Register adds the first entry).
    private volatile bool _isEmpty = true;

    // Set true when Register mutates state; cleared by Build. ExecuteAsync calls EnsureBuilt() to lazy-rebuild
    // if the frozen snapshot is stale.
    private volatile bool _dirty = false;

    // Monotonic counter bumped on every Build. Used by per-(TRequest,TResponse) static fast caches in
    // derived registries to invalidate cached entries when the frozen state changes.
    private int _generation;

    /// <summary>
    /// A monotonically increasing counter bumped by every successful <see cref="Build"/> call. Derived
    /// dispatch code uses this value to invalidate per-<c>(TRequest, TResponse)</c> caches when the
    /// frozen snapshot changes.
    /// </summary>
    internal int Generation => Volatile.Read(ref _generation);

    /// <inheritdoc/>
    public void Register(T pipelineBehaviorHandlerInfo)
    {
        lock (_lock)
        {
            if (pipelineBehaviorHandlerInfo.HandlerType.IsGenericTypeDefinition)
            {
                bool exists = false;
                for (int i = 0; i < _openGenericHandlers.Count; i++)
                {
                    if (_openGenericHandlers[i].HandlerInfo == pipelineBehaviorHandlerInfo)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    _openGenericHandlers.Add(new(pipelineBehaviorHandlerInfo, _registrationIndex++));
                    _isEmpty = false;
                    _dirty = true;
                }
                return;
            }

            if (!_pipelineBehaviors.TryGetValue(pipelineBehaviorHandlerInfo.RequestType, out var pipelineBehaviorValues))
                _pipelineBehaviors.Add(pipelineBehaviorHandlerInfo.RequestType, pipelineBehaviorValues = []);

            bool exists2 = false;
            for (int i = 0; i < pipelineBehaviorValues.Count; i++)
            {
                if (pipelineBehaviorValues[i].HandlerInfo == pipelineBehaviorHandlerInfo)
                {
                    exists2 = true;
                    break;
                }
            }
            if (!exists2)
            {
                pipelineBehaviorValues.Add(new(pipelineBehaviorHandlerInfo, _registrationIndex++));
                _isEmpty = false;
                _dirty = true;
            }
        }
    }

    /// <inheritdoc/>
    public void Build()
    {
        lock (_lock)
        {
            // Snapshot open-generic handlers, sorted by SortIndex descending.
            var openArr = _openGenericHandlers.ToArray();
            Array.Sort(openArr, static (a, b) => b.SortIndex.CompareTo(a.SortIndex));

            // Snapshot per-type specific handlers, sorted by SortIndex descending.
#if NET8_0_OR_GREATER
            var dict = new Dictionary<Type, PipelineBehaviorValue<T>[]>(_pipelineBehaviors.Count);
#else
            var dict = new Dictionary<Type, PipelineBehaviorValue<T>[]>(_pipelineBehaviors.Count);
#endif
            foreach (var kvp in _pipelineBehaviors)
            {
                var arr = kvp.Value.ToArray();
                Array.Sort(arr, static (a, b) => b.SortIndex.CompareTo(a.SortIndex));
                dict[kvp.Key] = arr;
            }

#if NET8_0_OR_GREATER
            _frozenPipelineBehaviors = dict.ToFrozenDictionary();
#else
            _frozenPipelineBehaviors = dict;
#endif
            _frozenOpenGenericHandlers = openArr;
            _dirty = false;
            Interlocked.Increment(ref _generation);
            OnBuilt();
        }
    }

    /// <summary>
    /// Ensures the frozen snapshot reflects the latest <see cref="Register"/> calls by invoking
    /// <see cref="Build"/> if any registration has occurred since the last build.
    /// </summary>
    protected void EnsureBuilt()
    {
        if (_dirty)
            Build();
    }

    /// <summary>
    /// Invoked at the end of <see cref="Build"/> whenever the frozen snapshot has been refreshed.
    /// Derived classes override this to invalidate caches that depend on the snapshot.
    /// </summary>
    protected virtual void OnBuilt() { }

    /// <inheritdoc/>
    public bool IsEmpty => _isEmpty;

    /// <summary>
    /// Read-only snapshot of registered open-generic pipeline behaviors, sorted by
    /// <see cref="PipelineBehaviorValue{THandlerInfo}.SortIndex"/> in descending order.
    /// Safe to access without locking once <see cref="Build"/> has run.
    /// </summary>
    protected PipelineBehaviorValue<T>[] FrozenOpenGenericHandlers => _frozenOpenGenericHandlers;

    /// <summary>
    /// Looks up the read-only snapshot of pipeline behaviors registered for a specific request type.
    /// </summary>
    /// <param name="requestType">The request type to look up.</param>
    /// <param name="values">When this method returns <see langword="true"/>, receives the array of behaviors
    /// registered for <paramref name="requestType"/>, sorted by
    /// <see cref="PipelineBehaviorValue{THandlerInfo}.SortIndex"/> in descending order.</param>
    /// <returns><see langword="true"/> if specific behaviors exist for <paramref name="requestType"/>;
    /// otherwise <see langword="false"/>.</returns>
    protected bool TryGetFrozenSpecific(Type requestType, out PipelineBehaviorValue<T>[] values)
    {
#if NET8_0_OR_GREATER
        return _frozenPipelineBehaviors.TryGetValue(requestType, out values!);
#else
        return _frozenPipelineBehaviors.TryGetValue(requestType, out values!);
#endif
    }

    /// <summary>
    /// Snapshot record describing a registered pipeline behavior together with its sort key.
    /// </summary>
    /// <typeparam name="THandlerInfo">The concrete handler-info type.</typeparam>
    protected readonly struct PipelineBehaviorValue<THandlerInfo> : IEquatable<PipelineBehaviorValue<THandlerInfo>>
        where THandlerInfo : PipelineBehaviorHandlerInfo
    {
        /// <summary>
        /// Initializes a new <see cref="PipelineBehaviorValue{THandlerInfo}"/>.
        /// </summary>
        /// <param name="handlerInfo">The pipeline-behavior handler information.</param>
        /// <param name="sortIndex">The fallback sort index used when <paramref name="handlerInfo"/> has
        /// no <see cref="Abstractions.Attributes.PipelineOverwritePriorityAttribute"/>. Attributed
        /// priorities take precedence and are stored negated so attributed behaviors always sort before
        /// non-attributed ones in descending-<see cref="SortIndex"/> order.</param>
        public PipelineBehaviorValue(THandlerInfo handlerInfo, int sortIndex)
        {
            HandlerInfo = handlerInfo;
            SortIndex = handlerInfo.TryGetPriority(out int priority) ? -priority : sortIndex;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return HandlerInfo.ToString();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is PipelineBehaviorValue<THandlerInfo> value && Equals(value);
        }

        /// <inheritdoc/>
        public bool Equals(PipelineBehaviorValue<THandlerInfo> other)
        {
            return EqualityComparer<PipelineBehaviorHandlerInfo>.Default.Equals(HandlerInfo, other.HandlerInfo);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
#if NET9_0_OR_GREATER
            return HashCode.Combine(HandlerInfo);
#else
            unchecked
            {
                return HandlerInfo?.GetHashCode() ?? 0;
            }
#endif
        }

        /// <summary>
        /// Gets the pipeline-behavior handler information.
        /// </summary>
        public THandlerInfo HandlerInfo { get; }

        /// <summary>
        /// Gets the sort key used to order behaviors during pipeline dispatch.
        /// </summary>
        public int SortIndex { get; }
    }
}

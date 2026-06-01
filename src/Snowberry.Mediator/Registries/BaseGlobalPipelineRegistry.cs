#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Diagnostics.CodeAnalysis;
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
    /// <summary>The lock guarding the mutable registration state.</summary>
#if NET9_0_OR_GREATER
    protected readonly Lock _lock = new();
#else
    protected readonly object _lock = new();
#endif

    /// <summary>Specific (closed) behaviors grouped by request type. Written under <see cref="_lock"/>.</summary>
    protected Dictionary<Type, List<PipelineBehaviorValue<T>>> _pipelineBehaviors = [];

    /// <summary>Open-generic behaviors. Written under <see cref="_lock"/>.</summary>
    protected List<PipelineBehaviorValue<T>> _openGenericHandlers = [];

    /// <summary>The next registration index, used as the fallback sort key for behaviors without a priority.</summary>
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

    // Optional compile-time-generated closed-type resolver. When set, it closes open-generic behaviors at
    // dispatch instead of Type.MakeGenericType, keeping the dispatch path NativeAOT-clean.
    private readonly Func<Type, Type, Type, Type>? _closedTypeResolver;

    /// <summary>Initializes a registry that closes open-generic behaviors via reflection.</summary>
    protected BaseGlobalPipelineRegistry()
    {
    }

    /// <summary>Initializes a registry that closes open-generic behaviors via a generated resolver.</summary>
    /// <param name="closedTypeResolver">Maps <c>(openHandlerType, requestType, responseType)</c> to the closed handler type.</param>
    protected BaseGlobalPipelineRegistry(Func<Type, Type, Type, Type>? closedTypeResolver)
    {
        _closedTypeResolver = closedTypeResolver;
    }

    /// <summary>
    /// Closes an open-generic behavior handler type for a given request/response pair. Uses the supplied
    /// closed-type resolver when present; otherwise falls back to <see cref="Type.MakeGenericType(System.Type[])"/>.
    /// </summary>
    /// <param name="openHandlerType">The open-generic behavior handler type definition.</param>
    /// <param name="requestType">The closed request type.</param>
    /// <param name="responseType">The closed response type.</param>
    /// <returns>The closed behavior handler type.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "A generated resolver supplies closed types in AOT scenarios; reflection is only used when no resolver is set.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "A generated resolver supplies closed types in AOT scenarios; reflection is only used when no resolver is set.")]
    protected Type CloseGeneric(Type openHandlerType, Type requestType, Type responseType)
        => _closedTypeResolver is { } resolver
            ? resolver(openHandlerType, requestType, responseType)
            : openHandlerType.MakeGenericType(requestType, responseType);

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
            // A concurrent slow-path builder may have already rebuilt the snapshot; avoid a redundant
            // re-freeze + Generation bump (which would needlessly invalidate the per-pair fast caches).
            if (!_dirty)
                return;

            // Snapshot open-generic handlers, sorted by SortIndex descending.
            var openArr = _openGenericHandlers.ToArray();
            Array.Sort(openArr, static (a, b) => b.SortIndex.CompareTo(a.SortIndex));

            // Snapshot per-type specific handlers, sorted by SortIndex descending.
            var dict = new Dictionary<Type, PipelineBehaviorValue<T>[]>(_pipelineBehaviors.Count);
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
        return _frozenPipelineBehaviors.TryGetValue(requestType, out values!);
    }

    /// <summary>
    /// Builds the closed behavior-type array for a request/response pair, in dispatch order (highest priority
    /// first), merging the per-request specific behaviors with the open-generic behaviors closed over the pair.
    /// </summary>
    /// <param name="requestType">The closed request type.</param>
    /// <param name="responseType">The closed response type.</param>
    /// <returns>The closed behavior types in dispatch order; an empty array when no behaviors apply.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "A generated resolver supplies closed types in AOT scenarios; reflection is only used when no resolver is set.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "A generated resolver supplies closed types in AOT scenarios; reflection is only used when no resolver is set.")]
    protected Type[] BuildBehaviorTypesFor(Type requestType, Type responseType)
    {
        bool hasSpecific = TryGetFrozenSpecific(requestType, out var specific);
        var openGeneric = FrozenOpenGenericHandlers;

        int specificLen = hasSpecific ? specific.Length : 0;
        int totalLen = specificLen + openGeneric.Length;
        if (totalLen == 0)
            return [];

        var result = new Type[totalLen];

        // Frozen arrays are sorted DESCENDING by SortIndex (lowest priority at index 0). The walker iterates
        // forward, so the OUTPUT must be ASCENDING by SortIndex (= HIGHEST priority first). Iterate both
        // inputs back-to-front, picking the lower SortIndex (= higher priority) at each step. Tie-break:
        // pick SPECIFIC so it lands at a lower output index (outer in the walker chain, runs first within
        // that priority level) - matches the previous "specifics processed before open-generics at each
        // priority level" ordering.
        int i = specificLen - 1;
        int j = openGeneric.Length - 1;
        int k = 0;
        while (i >= 0 || j >= 0)
        {
            bool pickSpecific;
            if (i < 0) pickSpecific = false;
            else if (j < 0) pickSpecific = true;
            else pickSpecific = specific[i].SortIndex <= openGeneric[j].SortIndex;

            if (pickSpecific)
            {
                result[k++] = specific[i].HandlerInfo.HandlerType;
                i--;
            }
            else
            {
                result[k++] = CloseGeneric(openGeneric[j].HandlerInfo.HandlerType, requestType, responseType);
                j--;
            }
        }

        return result;
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return HandlerInfo.ToString();
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
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;
using ZLinq;

namespace Snowberry.Mediator.Registries;

public class BaseGlobalPipelineRegistry<T> : IBaseGlobalPipelineRegistry<T>
    where T : PipelineBehaviorHandlerInfo
{
    protected Lock _lock = new();
    protected Dictionary<Type, List<PipelineBehaviorValue<T>>> _pipelineBehaviors = [];
    protected List<PipelineBehaviorValue<T>> _openGenericHandlers = [];
    protected int _registrationIndex = 0;

    /// <inheritdoc/>
    public void Register(T pipelineBehaviorHandlerInfo)
    {
        lock (pipelineBehaviorHandlerInfo)
        {
            if (pipelineBehaviorHandlerInfo.HandlerType.IsGenericTypeDefinition)
            {
                if (!_openGenericHandlers.AsValueEnumerable().Any(x => x.HandlerInfo == pipelineBehaviorHandlerInfo))
                    _openGenericHandlers.Add(new(pipelineBehaviorHandlerInfo, _registrationIndex++));

                return;
            }

            if (!_pipelineBehaviors.TryGetValue(pipelineBehaviorHandlerInfo.RequestType, out var pipelineBehaviorValues))
                _pipelineBehaviors.Add(pipelineBehaviorHandlerInfo.RequestType, pipelineBehaviorValues = []);

            if (!pipelineBehaviorValues.AsValueEnumerable().Any(x => x.HandlerInfo == pipelineBehaviorHandlerInfo))
                pipelineBehaviorValues.Add(new(pipelineBehaviorHandlerInfo, _registrationIndex++));
        }
    }

    /// <inheritdoc/>
    public bool IsEmpty
    {
        get
        {
            lock (_lock)
            {
                return _pipelineBehaviors.Count == 0 && _openGenericHandlers.Count == 0;
            }
        }
    }

    protected readonly struct PipelineBehaviorValue<THandlerInfo> : IEquatable<PipelineBehaviorValue<THandlerInfo>>
        where THandlerInfo : PipelineBehaviorHandlerInfo
    {
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
            return HashCode.Combine(HandlerInfo);
        }

        public THandlerInfo HandlerInfo { get; }

        public int SortIndex { get; }
    }
}

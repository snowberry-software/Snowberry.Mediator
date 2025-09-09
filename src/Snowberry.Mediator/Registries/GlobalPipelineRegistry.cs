using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;
using ZLinq;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// The global pipeline registry implementation.
/// </summary>
public class GlobalPipelineRegistry : BaseGlobalPipelineRegistry<PipelineBehaviorHandlerInfo>, IGlobalPipelineRegistry
{
    /// <inheritdoc/>
    public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(IServiceProvider serviceProvider, IRequestHandler<TRequest, TResponse> handler, TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        if (IsEmpty)
            return handler.HandleAsync(request, cancellationToken);

        var requestType = typeof(TRequest);

        // Get specific handlers for this request type
        _pipelineBehaviors.TryGetValue(requestType, out var behaviorValues);

        // If we only have specific handlers, use the fast path
        if (_openGenericHandlers.Count == 0 && behaviorValues != null)
        {
            PipelineHandlerDelegate<TRequest, TResponse> next = handler.HandleAsync;

            var sortedHandlers = behaviorValues
                .AsValueEnumerable()
                .OrderByDescending(x => x.SortIndex);

            foreach (var pipelineBehavior in sortedHandlers)
            {
                var current = Unsafe.As<IPipelineBehavior<TRequest, TResponse>>(
                    serviceProvider.GetService(pipelineBehavior.HandlerInfo.HandlerType)
                    ?? throw new PipelineBehaviorNotFoundException(typeof(TRequest), isStream: false));

                current.NextPipeline = next;
                next = current.HandleAsync;
            }

            return next(request, cancellationToken);
        }

        // If we only have open generic handlers, process them directly
        if (behaviorValues == null || behaviorValues.Count == 0)
        {
            PipelineHandlerDelegate<TRequest, TResponse> next = handler.HandleAsync;

            // Process open generic handlers in reverse order (by SortIndex)
            var sortedHandlers = _openGenericHandlers
                .AsValueEnumerable()
                .OrderByDescending(x => x.SortIndex);

            var responseType = typeof(TResponse);

            foreach (var openGenericHandler in sortedHandlers)
            {
                var handlerType = openGenericHandler.HandlerInfo.HandlerType;
                var current = Unsafe.As<IPipelineBehavior<TRequest, TResponse>>(serviceProvider.GetService(handlerType.MakeGenericType(requestType, responseType)))!;
                current.NextPipeline = next;
                next = current.HandleAsync;
            }

            return next(request, cancellationToken);
        }

        // Mixed case: we have both specific and open generic handlers
        // Process them by priority without creating any temporary collections
        PipelineHandlerDelegate<TRequest, TResponse> finalNext = handler.HandleAsync;

        // Find the highest priority first, then work backwards
        // This avoids any allocations by processing handlers multiple times
        int maxPriority = int.MinValue;
        int minPriority = int.MaxValue;

        // First pass: find priority range from specific handlers
        if (behaviorValues != null)
        {
            for (int i = 0; i < behaviorValues.Count; i++)
            {
                var specificHandler = behaviorValues[i];
                int priority = specificHandler.SortIndex;

                if (priority > maxPriority)
                    maxPriority = priority;
                if (priority < minPriority)
                    minPriority = priority;
            }
        }

        // First pass: find priority range from open generic handlers  
        for (int i = 0; i < _openGenericHandlers.Count; i++)
        {
            var openGenericHandler = _openGenericHandlers[i];
            int priority = openGenericHandler.SortIndex;

            if (priority > maxPriority)
                maxPriority = priority;
            if (priority < minPriority)
                minPriority = priority;
        }

        // Process handlers from highest to lowest priority (reverse order for pipeline building)
        for (int currentPriority = maxPriority; currentPriority >= minPriority; currentPriority--)
        {
            // Process specific handlers at this priority level
            if (behaviorValues != null)
            {
                for (int i = 0; i < behaviorValues.Count; i++)
                {
                    var specificHandler = behaviorValues[i];

                    if (specificHandler.SortIndex != currentPriority)
                        continue;

                    var current = Unsafe.As<IPipelineBehavior<TRequest, TResponse>>(
                        serviceProvider.GetService(specificHandler.HandlerInfo.HandlerType)
                        ?? throw new PipelineBehaviorNotFoundException(typeof(TRequest), isStream: false));

                    current.NextPipeline = finalNext;
                    finalNext = current.HandleAsync;
                }
            }

            var responseType = typeof(TResponse);

            for (int i = 0; i < _openGenericHandlers.Count; i++)
            {
                var openGenericHandler = _openGenericHandlers[i];

                if (openGenericHandler.SortIndex != currentPriority)
                    continue;

                var handlerType = openGenericHandler.HandlerInfo.HandlerType;

                var current = Unsafe.As<IPipelineBehavior<TRequest, TResponse>>(serviceProvider.GetService(handlerType.MakeGenericType(requestType, responseType)))!;

                current.NextPipeline = finalNext;
                finalNext = current.HandleAsync;
            }
        }

        return finalNext(request, cancellationToken);
    }
}

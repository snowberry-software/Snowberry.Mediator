using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.Registries;

/// <summary>
/// Default <see cref="IGlobalStreamPipelineRegistry"/> implementation. Tracks
/// <see cref="Abstractions.Pipeline.IStreamPipelineBehavior{TRequest, TResponse}"/> registrations and
/// dispatches stream requests through them in priority order, resolving each behavior from the supplied
/// <see cref="IServiceProvider"/> on every call.
/// </summary>
public sealed class GlobalStreamPipelineRegistry : BaseGlobalPipelineRegistry<StreamPipelineBehaviorHandlerInfo>, IGlobalStreamPipelineRegistry
{
    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.")]
    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.")]
    public IAsyncEnumerable<TResponse> ExecuteAsync<TRequest, TResponse>(IServiceProvider serviceProvider, IStreamRequestHandler<TRequest, TResponse> handler, TRequest request, CancellationToken cancellationToken)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
    {
        if (IsEmpty)
            return handler.HandleAsync(request, cancellationToken);

        EnsureBuilt();

        var requestType = typeof(TRequest);
        bool hasSpecific = TryGetFrozenSpecific(requestType, out var specific);
        var openGeneric = FrozenOpenGenericHandlers;

        if (openGeneric.Length == 0 && hasSpecific)
        {
            StreamPipelineHandlerDelegate<TRequest, TResponse> next = handler.HandleAsync;
            for (int i = 0; i < specific.Length; i++)
            {
                var current = Unsafe.As<IStreamPipelineBehavior<TRequest, TResponse>>(
                    serviceProvider.GetService(specific[i].HandlerInfo.HandlerType)
                    ?? throw new PipelineBehaviorNotFoundException(typeof(TRequest), isStream: true));
                current.NextPipeline = next;
                next = current.HandleAsync;
            }
            return next(request, cancellationToken);
        }

        if (!hasSpecific)
        {
            StreamPipelineHandlerDelegate<TRequest, TResponse> next = handler.HandleAsync;
            var responseType = typeof(TResponse);
            for (int i = 0; i < openGeneric.Length; i++)
            {
                var handlerType = openGeneric[i].HandlerInfo.HandlerType;
                var current = Unsafe.As<IStreamPipelineBehavior<TRequest, TResponse>>(
                    serviceProvider.GetService(handlerType.MakeGenericType(requestType, responseType)))!;
                current.NextPipeline = next;
                next = current.HandleAsync;
            }
            return next(request, cancellationToken);
        }

        return ExecuteMixed(serviceProvider, handler, specific, openGeneric, request, requestType, cancellationToken);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.")]
    private static IAsyncEnumerable<TResponse> ExecuteMixed<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        IStreamRequestHandler<TRequest, TResponse> handler,
        PipelineBehaviorValue<StreamPipelineBehaviorHandlerInfo>[] specific,
        PipelineBehaviorValue<StreamPipelineBehaviorHandlerInfo>[] openGeneric,
        TRequest request,
        Type requestType,
        CancellationToken cancellationToken)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
    {
        var responseType = typeof(TResponse);
        StreamPipelineHandlerDelegate<TRequest, TResponse> next = handler.HandleAsync;

        // Same merge invariant as GlobalPipelineRegistry: iterate forward, pick higher SortIndex (lower
        // effective priority) first so the highest-priority behavior ends up outermost. Tie-break to specific.
        int i = 0;
        int j = 0;
        while (i < specific.Length || j < openGeneric.Length)
        {
            bool pickSpecific;
            if (i >= specific.Length) pickSpecific = false;
            else if (j >= openGeneric.Length) pickSpecific = true;
            else pickSpecific = specific[i].SortIndex >= openGeneric[j].SortIndex;

            IStreamPipelineBehavior<TRequest, TResponse> current;
            if (pickSpecific)
            {
                current = Unsafe.As<IStreamPipelineBehavior<TRequest, TResponse>>(
                    serviceProvider.GetService(specific[i].HandlerInfo.HandlerType)
                    ?? throw new PipelineBehaviorNotFoundException(typeof(TRequest), isStream: true));
                i++;
            }
            else
            {
                var handlerType = openGeneric[j].HandlerInfo.HandlerType;
                current = Unsafe.As<IStreamPipelineBehavior<TRequest, TResponse>>(
                    serviceProvider.GetService(handlerType.MakeGenericType(requestType, responseType)))!;
                j++;
            }
            current.NextPipeline = next;
            next = current.HandleAsync;
        }

        return next(request, cancellationToken);
    }
}

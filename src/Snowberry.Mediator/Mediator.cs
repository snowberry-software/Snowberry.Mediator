using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator;

/// <inheritdoc cref="IMediator"/>.
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGlobalPipelineRegistry? _pipelineRegistry;
    private readonly IGlobalStreamPipelineRegistry? _streamPipelineRegistry;
    private readonly IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>? _notificationRegistry;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Registries are registered as singleton instances in DI — resolve once at construction so per-call
        // dispatch never pays the GetService cost. Each is optional; null = no behaviors/handlers of that kind.
        _pipelineRegistry = Unsafe.As<IGlobalPipelineRegistry?>(serviceProvider.GetService(typeof(IGlobalPipelineRegistry)));
        _streamPipelineRegistry = Unsafe.As<IGlobalStreamPipelineRegistry?>(serviceProvider.GetService(typeof(IGlobalStreamPipelineRegistry)));
        _notificationRegistry = Unsafe.As<IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>?>(serviceProvider.GetService(typeof(IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>)));
    }

    /// <inheritdoc/>
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(IRequest<TRequest, TResponse> request, CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        cancellationToken.ThrowIfCancellationRequested();

        object service = _serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) ?? throw new HandlerNotFoundException(typeof(TRequest), isStream: false);

        var requestTyped = Unsafe.As<TRequest>(request);
        var handler = Unsafe.As<IRequestHandler<TRequest, TResponse>>(service);

        var pipelineRegistry = _pipelineRegistry;
        if (pipelineRegistry is not null && !pipelineRegistry.IsEmpty)
            return pipelineRegistry.ExecuteAsync(_serviceProvider, handler, requestTyped, cancellationToken);

        return handler.HandleAsync(requestTyped, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TResponse> CreateStreamAsync<TRequest, TResponse>(IStreamRequest<TRequest, TResponse> request, CancellationToken cancellationToken = default)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        cancellationToken.ThrowIfCancellationRequested();

        object service = _serviceProvider.GetService(typeof(IStreamRequestHandler<TRequest, TResponse>)) ?? throw new HandlerNotFoundException(typeof(TRequest), isStream: true);

        var requestTyped = Unsafe.As<TRequest>(request);
        var handler = Unsafe.As<IStreamRequestHandler<TRequest, TResponse>>(service);

        var streamPipelineRegistry = _streamPipelineRegistry;
        if (streamPipelineRegistry is not null && !streamPipelineRegistry.IsEmpty)
            return streamPipelineRegistry.ExecuteAsync(_serviceProvider, handler, requestTyped, cancellationToken);

        return handler.HandleAsync(requestTyped, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        _ = notification ?? throw new ArgumentNullException(nameof(notification));

        cancellationToken.ThrowIfCancellationRequested();

        var registry = _notificationRegistry;
        if (registry is null)
            throw new NotificationHandlerNotFoundException(typeof(TNotification));

        return registry.PublishAsync(_serviceProvider, notification, cancellationToken);
    }
}

using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator;

/// <inheritdoc cref="IMediator"/>.
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(IRequest<TRequest, TResponse> request, CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        cancellationToken.ThrowIfCancellationRequested();

        object service = _serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) ?? throw new HandlerNotFoundException(typeof(TRequest), isStream: false);

        var requestTyped = Unsafe.As<TRequest>(request);

        var pipelineRegistry = Unsafe.As<IGlobalPipelineRegistry?>(_serviceProvider.GetService(typeof(IGlobalPipelineRegistry)));
        var handler = Unsafe.As<IRequestHandler<TRequest, TResponse>>(service);

        if (pipelineRegistry is not null)
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

        var streamPipelineRegistry = Unsafe.As<IGlobalStreamPipelineRegistry?>(_serviceProvider.GetService(typeof(IGlobalStreamPipelineRegistry)));
        var handler = Unsafe.As<IStreamRequestHandler<TRequest, TResponse>>(service);

        if (streamPipelineRegistry is not null)
            return streamPipelineRegistry.ExecuteAsync(_serviceProvider, handler, requestTyped, cancellationToken);

        return handler.HandleAsync(requestTyped, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        _ = notification ?? throw new ArgumentNullException(nameof(notification));

        cancellationToken.ThrowIfCancellationRequested();

        var registry = Unsafe.As<IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>?>(_serviceProvider.GetService(typeof(IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>)));

        if (registry == null)
            throw new NotificationHandlerNotFoundException(typeof(TNotification));

        return registry.PublishAsync(_serviceProvider, notification, cancellationToken);
    }
}

# Snowberry.Mediator.Abstractions

Core abstractions for `Snowberry.Mediator`: interfaces, marker types, and attributes consumed by handler implementations, pipeline behaviors, and notification handlers.

Reference this package directly only when authoring a library that defines handlers, behaviors, or notifications without depending on the main mediator implementation. Most consumers should reference `Snowberry.Mediator` (or one of the DI integration packages) instead.

## Contents

- `IMediator`, `IMediatorSender`, `IMediatorPublisher`: the mediator surface area.
- `IRequest<TRequest, TResponse>`, `IStreamRequest<TRequest, TResponse>`, `INotification`: message marker interfaces.
- `IRequestHandler<TRequest, TResponse>`, `IStreamRequestHandler<TRequest, TResponse>`, `INotificationHandler<TNotification>`: handler contracts.
- `IPipelineBehavior<TRequest, TResponse>`, `IStreamPipelineBehavior<TRequest, TResponse>`: pipeline behavior contracts.
- `IPipelineContinuation<TRequest, TResponse>`, `IStreamPipelineContinuation<TRequest, TResponse>`: continuation types used by pipeline behaviors.
- `PipelineOverwritePriorityAttribute`: sets the execution order of pipeline behaviors.
- Exception types: `HandlerNotFoundException`, `NotificationHandlerNotFoundException`, `NotificationHandlerNotResolvedException`.

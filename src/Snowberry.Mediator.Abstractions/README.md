# Snowberry.Mediator.Abstractions

**The contracts of [`Snowberry.Mediator`](../../README.md).** This package holds the message, handler and pipeline interfaces, the `IMediator` surface, the priority attribute and the exception types. It targets `net11.0`, `net10.0` and `netstandard2.0`, and has no dependencies on modern .NET (on `netstandard2.0` it uses [`System.Threading.Tasks.Extensions`](https://www.nuget.org/packages/System.Threading.Tasks.Extensions) and [`Microsoft.Bcl.AsyncInterfaces`](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces) for `ValueTask` and `IAsyncEnumerable`).

Reference it directly when a library only defines messages, handlers or behaviors and should not depend on the mediator implementation. Applications reference a DI integration package instead, which brings this package in.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Type Catalog](#type-catalog)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.Abstractions
```

## Quick Start

Define a request and its handler in a library that only references the abstractions:

```csharp
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;

public sealed record GetUser(int Id) : IRequest<GetUser, string>;

public sealed class GetUserHandler : IRequestHandler<GetUser, string>
{
    public ValueTask<string> HandleAsync(GetUser request, CancellationToken cancellationToken = default)
        => new($"user-{request.Id}");
}
```

The application registers `GetUserHandler` through a DI integration and calls `IMediator.SendAsync(new GetUser(42))`.

## Type Catalog

| Type | Namespace | Purpose |
| --- | --- | --- |
| `IMediator` | `Snowberry.Mediator.Abstractions` | The mediator; combines `IMediatorSender` and `IMediatorPublisher`. The only one registered in DI. |
| `IMediatorSender` | `Snowberry.Mediator.Abstractions.Mediator` | `SendAsync` and `CreateStreamAsync`. |
| `IMediatorPublisher` | `Snowberry.Mediator.Abstractions.Mediator` | `PublishAsync`. |
| `Empty` | `Snowberry.Mediator.Abstractions` | Unit response type for requests without a result (`Empty.Value`, `Empty.ValueTask`). |
| `IRequest<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Messages` | Marks a request; `TRequest` is the request type itself and must be a class. |
| `IStreamRequest<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Messages` | Marks a stream request. |
| `INotification` | `Snowberry.Mediator.Abstractions.Messages` | Marks a notification. |
| `IRequestHandler<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Handler` | Handles a request: `ValueTask<TResponse> HandleAsync(request, ct)`. |
| `IStreamRequestHandler<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Handler` | Handles a stream request: `IAsyncEnumerable<TResponse> HandleAsync(request, ct)`. |
| `INotificationHandler<TNotification>` | `Snowberry.Mediator.Abstractions.Handler` | Handles a notification: `ValueTask HandleAsync(notification, ct)`. |
| `IPipelineBehavior<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Pipeline` | Wraps request handling; receives the next step as `TNext : struct, IPipelineContinuation<,>`. |
| `IPipelineContinuation<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Pipeline` | The next step of a request pipeline (`InvokeAsync`). |
| `IStreamPipelineBehavior<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Pipeline` | Wraps stream handling. |
| `IStreamPipelineContinuation<TRequest, TResponse>` | `Snowberry.Mediator.Abstractions.Pipeline` | The next step of a stream pipeline. |
| `PipelineOverwritePriorityAttribute` | `Snowberry.Mediator.Abstractions.Attributes` | `Priority` moves a behavior ahead of the registration order; higher runs earlier. |
| `HandlerNotFoundException` | `Snowberry.Mediator.Abstractions.Exceptions` | No handler for a request or stream request (`RequestType`, `IsStream`). |
| `NotificationHandlerNotFoundException` | `Snowberry.Mediator.Abstractions.Exceptions` | `PublishAsync` with no notification handlers registered at all (`NotificationType`). |
| `NotificationHandlerNotResolvedException` | `Snowberry.Mediator.Abstractions.Exceptions` | No handler could be resolved for this notification type (`NotificationType`, `HandlerType`). |

## Pitfalls

- **The request names itself.** Write `record GetUser : IRequest<GetUser, string>`, not `IRequest<string>`. The self-reference lets `SendAsync` infer both type arguments.
- **Requests must be reference types.** `TRequest` is constrained to `class`, so a `struct` request does not compile.
- **Positive priorities only.** Zero and negative `Priority` values interleave with unattributed behaviors instead of running last; see [ordering and priority](../../README.md#ordering-and-priority).
- **Defining a handler does not register it.** Registration happens in the application through a DI integration or the source generator.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation and registries. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) | `Microsoft.Extensions.DependencyInjection` integration. |
| [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) | `Snowberry.DependencyInjection` integration. |
| [`Snowberry.Mediator.SourceGenerator`](../Snowberry.Mediator.SourceGenerator/README.md) | Compile-time, reflection-free registration. |

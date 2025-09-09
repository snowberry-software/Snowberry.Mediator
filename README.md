[![License](https://img.shields.io/github/license/snowberry-software/Snowberry.Mediator)](https://github.com/snowberry-software/Snowberry.Mediator/blob/master/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/Snowberry.Mediator.svg?logo=nuget)](https://www.nuget.org/packages/Snowberry.Mediator/)

Lightweight mediator implementation (request/response, streaming requests and notifications) with support for pipeline behaviors and pluggable registration via assembly scanning or explicit type lists.

Use this package to decouple request/response handlers, streaming request handlers and notification handlers from callers, and to add cross-cutting pipeline behaviors.

# Usage

Register an `IMediator` implementation and handlers (either by scanning assemblies or by specifying types) and call into the mediator using the `IMediator` surface:

- Send a request: `ValueTask<TResponse> SendAsync<TRequest, TResponse>(IRequest<TRequest, TResponse> request, CancellationToken)`
- Create a stream: `IAsyncEnumerable<TResponse> CreateStreamAsync<TRequest, TResponse>(IStreamRequest<TRequest, TResponse> request, CancellationToken)`
- Publish a notification: `ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken)`

The mediator resolves handlers from an `IServiceProvider` (provided at construction) and can execute optional pipeline behavior chains when a global pipeline registry is registered.

# Features

- Core contracts and supported types (with generic signatures):
  - `IRequest<TRequest, TResponse>` / `IRequestHandler<TRequest, TResponse>`
  - `IStreamRequest<TRequest, TResponse>` / `IStreamRequestHandler<TRequest, TResponse>`
  - `INotification` / `INotificationHandler<TNotification>`
  - `IPipelineBehavior<TRequest, TResponse>`
  - `IStreamPipelineBehavior<TRequest, TResponse>`
- Assembly scanning helper to discover handlers, pipeline behaviors and notification handlers (`MediatorAssemblyHelper`).
- Global registries for pipeline and notification handlers used at runtime by `Mediator`.

## Examples

Below are minimal examples demonstrating common usage patterns.

### Microsoft Dependency Injection

The repository contains an integration extension (`Snowberry.Mediator.Extensions.DependencyInjection`) which exposes `AddSnowberryMediator` to register the mediator and handlers into an `IServiceCollection`.

Example: register by scanning the current assembly and enable pipeline/notification scanning:

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSnowberryMediator(options =>
{
    options.Assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
    options.ScanNotificationHandlers = true;
    options.ScanPipelineBehaviors = true;
});

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<Snowberry.Mediator.Abstractions.IMediator>();

// Send a request (example)
// await mediator.SendAsync(new MyRequest(...));
```

Example: explicit registration of pipeline behaviors (ordered) and notification handlers (no scanning):

```csharp
services.AddSnowberryMediator(opts =>
{
    opts.Assemblies = new List<Assembly>();
    opts.RegisterPipelineBehaviors = true;
    opts.PipelineBehaviorTypes = new List<Type>
    {
        typeof(MyApp.Pipeline.LoggingBehavior<,>),
        typeof(MyApp.Pipeline.ValidationBehavior<,>)
    };

    opts.RegisterNotificationHandlers = true;
    opts.NotificationHandlerTypes = new List<Type>
    {
        typeof(MyApp.Notifications.SomeNotificationHandler)
    };
});
```

## Pipeline behavior ordering and priority

Pipeline behaviors are executed in a linked delegate chain. The order in which behaviors are executed matters because each behavior receives a `NextPipeline` delegate that it must call to continue the chain.

Ordering rules:

- When you explicitly provide `PipelineBehaviorTypes` (or `StreamPipelineBehaviorTypes`), the order of types in the list is preserved and used as the base ordering.
- When pipeline behaviors are discovered via scanning, or when combining scanned and explicit lists, the framework can use the `PipelineOverwritePriorityAttribute` applied to behavior types to determine priority.
- `PipelineOverwritePriorityAttribute` contains an integer `Priority` property. Higher values indicate higher priority — behaviors with a higher `Priority` value are executed earlier in the pipeline chain.

Practical guidance:

- If you need a behavior to run first (for example, logging or diagnostics that should wrap everything), give it a higher `Priority`.
- If you mix scanned behaviors and an explicit list, set priorities on scanned implementations when their relative position matters, or prefer explicit ordering for predictable placement.
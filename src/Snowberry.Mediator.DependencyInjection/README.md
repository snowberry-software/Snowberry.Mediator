# Snowberry.Mediator.DependencyInjection

`Snowberry.DependencyInjection` integration for `Snowberry.Mediator`.

Adds `AddSnowberryMediator` and `AppendSnowberryMediator` to `IServiceRegistry`, registering the mediator, request and stream handlers, pipeline behaviors, and notification handlers.

For `Microsoft.Extensions.DependencyInjection` consumers, use the `Snowberry.Mediator.Extensions.DependencyInjection` package instead.

## Usage

```csharp
using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.Mediator.DependencyInjection;

using var container = new ServiceContainer();

container.AddSnowberryMediator(opt =>
{
    opt.RequestHandlerTypes        = [typeof(MyRequestHandler)];
    opt.PipelineBehaviorTypes      = [typeof(MyPipelineBehavior<,>)];
    opt.NotificationHandlerTypes   = [typeof(MyNotificationHandler)];
    opt.StreamRequestHandlerTypes  = [typeof(MyStreamRequestHandler)];
    opt.StreamPipelineBehaviorTypes = [typeof(MyStreamPipelineBehavior<,>)];
}, ServiceLifetime.Scoped);
```

Assembly scanning is also supported via `opt.Assemblies` but requires unreferenced code and is not AOT-compatible.

## `AddSnowberryMediator` vs `AppendSnowberryMediator`

Both extensions register the same set of services and use try-register semantics. Neither evicts an already-registered `IMediator` or handler.

- `AddSnowberryMediator` is the standard call. Use it once per container.
- `AppendSnowberryMediator` is intended for adding more handlers, behaviors, or notification handlers to a container that has already been configured with `AddSnowberryMediator` (for example, when a downstream module contributes additional handlers). When a global pipeline or notification registry is already present it fetches the existing instance and adds the new handlers to it, so previously registered handlers are preserved.

## OpenTelemetry instrumentation

Pair with `Snowberry.Mediator.OpenTelemetry` to emit OpenTelemetry traces and metrics for every mediator dispatch. Note that for this container, `AddSnowberryMediatorOpenTelemetry` must be called **before** `AddSnowberryMediator`.

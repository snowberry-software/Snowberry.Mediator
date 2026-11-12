# Snowberry.Mediator.DependencyInjection

**The [`Snowberry.DependencyInjection`](https://github.com/snowberry-software/Snowberry.DependencyInjection) integration for [`Snowberry.Mediator`](../../README.md).** It adds `AddSnowberryMediator` and `AppendSnowberryMediator` to `IServiceRegistry`, registering `IMediator`, request and stream handlers, pipeline behaviors and notification handlers by assembly scanning or explicit type lists. It targets `net11.0`, `net10.0` and `netstandard2.0` and depends on `Snowberry.DependencyInjection` 6.

For `Microsoft.Extensions.DependencyInjection` use [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) instead. For trimmed or Native AOT apps, pair this package with the [source generator](../Snowberry.Mediator.SourceGenerator/README.md).

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [AddSnowberryMediator vs AppendSnowberryMediator](#addsnowberrymediator-vs-appendsnowberrymediator)
- [OpenTelemetry](#opentelemetry)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.DependencyInjection
```

## Quick Start

```csharp
using Snowberry.DependencyInjection;                          // ServiceContainer
using Snowberry.DependencyInjection.Abstractions;             // ServiceLifetime
using Snowberry.DependencyInjection.Abstractions.Extensions;  // GetRequiredService
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;

using var container = new ServiceContainer();

container.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(MyRequestHandler)];
    options.StreamRequestHandlerTypes = [typeof(MyStreamRequestHandler)];
    options.NotificationHandlerTypes = [typeof(MyNotificationHandler)];
    options.PipelineBehaviorTypes = [typeof(MyPipelineBehavior<,>)];
    options.StreamPipelineBehaviorTypes = [typeof(MyStreamPipelineBehavior<,>)];
}, ServiceLifetime.Scoped);

var mediator = container.GetRequiredService<IMediator>();
```

The `serviceLifetime` parameter defaults to `Scoped` and applies to `IMediator` and every handler and behavior. Assembly scanning works through `options.Assemblies` plus the `Scan*` flags, as described in the [main README](../../README.md#registration).

## AddSnowberryMediator vs AppendSnowberryMediator

Both register through try-register semantics: an existing `IMediator` or handler is never replaced.

- **`AddSnowberryMediator`** is the standard call. A second call that would register behaviors or notification handlers while a registry already exists throws `InvalidOperationException`, so a module cannot silently create a second, unused registry.
- **`AppendSnowberryMediator`** adds handlers, behaviors and notification handlers to the existing registries (for example from a plugin or feature module). It also works as the first call, registering `IMediator` when it is missing.

```csharp
container.AppendSnowberryMediator(options =>
{
    options.NotificationHandlerTypes = [typeof(PluginNotificationHandler)];
});
```

## OpenTelemetry

Add [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) and call `AddSnowberryMediatorOpenTelemetry` **before** `AddSnowberryMediator`. This is the reverse of the Microsoft DI order: the decorator has to claim the `IMediator` slot first, and calling it afterwards throws `InvalidOperationException`.

## Pitfalls

- **These entry points are not AOT-safe, even with explicit lists.** Both carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so trimmed and AOT builds warn. Use the source generator, whose `AddSnowberryMediator(lifetime, append)` for `IServiceRegistry` is emitted in this namespace.
- **Append needs a registry it can read back.** Appending reuses the existing registries by resolving them from the container; a custom `IServiceRegistry` that is not also an `IKeyedServiceProvider` cannot do that, and the appended behaviors and notification handlers are lost. `ServiceContainer` supports it.
- **Order matters with OpenTelemetry.** Register the OpenTelemetry decorator first on this container.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation. |
| [`Snowberry.Mediator.SourceGenerator`](../Snowberry.Mediator.SourceGenerator/README.md) | Generated, AOT-safe `AddSnowberryMediator()` for `IServiceRegistry`. |
| [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) | OpenTelemetry decorator for this container. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) | The equivalent integration for `Microsoft.Extensions.DependencyInjection`. |
| [`Snowberry.Mediator.DependencyInjection.Shared`](../Snowberry.Mediator.DependencyInjection.Shared/README.md) | The container-agnostic helper this package builds on. |

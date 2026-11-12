# Snowberry.Mediator.Extensions.DependencyInjection

**The [`Microsoft.Extensions.DependencyInjection`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions) integration for [`Snowberry.Mediator`](../../README.md).** It adds `AddSnowberryMediator` to `IServiceCollection`, registering `IMediator`, request and stream handlers, pipeline behaviors and notification handlers by assembly scanning or explicit type lists. It targets `net11.0`, `net10.0` and `netstandard2.0`.

For the `Snowberry.DependencyInjection` container use [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) instead. For trimmed or Native AOT apps, pair this package with the [source generator](../Snowberry.Mediator.SourceGenerator/README.md).

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Registration Options](#registration-options)
- [Adding Handlers Later](#adding-handlers-later)
- [OpenTelemetry](#opentelemetry)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.Extensions.DependencyInjection
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(MyRequestHandler)];
    options.StreamRequestHandlerTypes = [typeof(MyStreamRequestHandler)];
    options.NotificationHandlerTypes = [typeof(MyNotificationHandler)];
    options.PipelineBehaviorTypes = [typeof(MyPipelineBehavior<,>)];
    options.StreamPipelineBehaviorTypes = [typeof(MyStreamPipelineBehavior<,>)];
});

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
```

`ServiceCollection` and `BuildServiceProvider` come from the `Microsoft.Extensions.DependencyInjection` package; this integration itself only depends on the abstractions.

## Registration Options

```csharp
public static IServiceCollection AddSnowberryMediator(
    this IServiceCollection services,
    Action<MediatorOptions> configure,
    ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
    bool append = false)
```

- **`serviceLifetime`** applies to `IMediator` and every handler and behavior. The global registries are singletons.
- **Assembly scanning** registers request and stream handlers from `options.Assemblies`; set `ScanPipelineBehaviors`, `ScanStreamPipelineBehaviors` and `ScanNotificationHandlers` to scan the other categories too:

  ```csharp
  services.AddSnowberryMediator(options =>
  {
      options.Assemblies = [typeof(MyRequestHandler).Assembly];
      options.ScanPipelineBehaviors = true;
      options.ScanNotificationHandlers = true;
  });
  ```

- **Explicit lists** keep the order of `PipelineBehaviorTypes` / `StreamPipelineBehaviorTypes`, unless a behavior carries `[PipelineOverwritePriority]`.

The full `MediatorOptions` table is in the [main README](../../README.md#registration).

## Adding Handlers Later

Registration uses `TryAdd`: an existing `IMediator` or handler is never replaced. A second call that would register behaviors or notification handlers while a registry already exists throws `InvalidOperationException`. Pass `append: true` to add handlers to the existing registries instead, for example from a plugin or feature module:

```csharp
services.AddSnowberryMediator(options =>
{
    options.NotificationHandlerTypes = [typeof(PluginNotificationHandler)];
}, append: true);
```

## OpenTelemetry

Add [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) and call `AddSnowberryMediatorOpenTelemetry` **after** `AddSnowberryMediator`; calling it first throws `InvalidOperationException`.

## Pitfalls

- **This entry point is not AOT-safe, even with explicit lists.** It carries `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` because it inspects handler interfaces and closes generic types at runtime, so trimmed and AOT builds warn (`IL2026`, `IL3050`). Use the source generator's `AddSnowberryMediator()` there.
- **Scanning skips behaviors and notification handlers by default.** Without the `Scan*` flags only request and stream handlers are found.
- **Second calls need `append: true`.** Otherwise adding behaviors or notification handlers a second time throws.
- **There is no `AppendSnowberryMediator` here.** That method belongs to the `Snowberry.DependencyInjection` integration; this package uses the `append` parameter.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation. |
| [`Snowberry.Mediator.SourceGenerator`](../Snowberry.Mediator.SourceGenerator/README.md) | Generated, AOT-safe `AddSnowberryMediator()` for `IServiceCollection`. |
| [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) | OpenTelemetry decorator for this container. |
| [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) | The equivalent integration for `Snowberry.DependencyInjection`. |
| [`Snowberry.Mediator.DependencyInjection.Shared`](../Snowberry.Mediator.DependencyInjection.Shared/README.md) | The container-agnostic helper this package builds on. |

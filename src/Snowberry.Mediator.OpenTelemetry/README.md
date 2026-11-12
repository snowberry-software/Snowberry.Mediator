# Snowberry.Mediator.OpenTelemetry

**OpenTelemetry for [`Snowberry.Mediator`](../../README.md) on [`Snowberry.DependencyInjection`](https://github.com/snowberry-software/Snowberry.DependencyInjection).** `AddSnowberryMediatorOpenTelemetry` registers the `InstrumentedMediator` decorator from [`Snowberry.Mediator.OpenTelemetry.Shared`](../Snowberry.Mediator.OpenTelemetry.Shared/README.md) as the `IMediator` of an `IServiceRegistry`, so every dispatch emits spans and metrics. It targets `net11.0`, `net10.0` and `netstandard2.0`.

For `Microsoft.Extensions.DependencyInjection` use [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) instead.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Order of Registration](#order-of-registration)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.OpenTelemetry
dotnet add package OpenTelemetry                    # Sdk.CreateTracerProviderBuilder()
dotnet add package OpenTelemetry.Exporter.Console   # or the exporter you use
```

## Quick Start

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.OpenTelemetry;

using var container = new ServiceContainer();

// 1. Register the decorator FIRST.
container.AddSnowberryMediatorOpenTelemetry(options =>
{
    options.EnablePipelineBehaviorSpans = true;     // optional, off by default
    options.EnableNotificationHandlerSpans = true;  // optional, off by default
}, lifetime: ServiceLifetime.Scoped);

// 2. Then register the mediator with the same lifetime.
container.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(MyRequestHandler)];
}, ServiceLifetime.Scoped);

// 3. Subscribe the OpenTelemetry SDK to the mediator sources and meter.
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();
```

The full options table, the emitted activities and metrics, and the enrichment hooks are documented in the [`Snowberry.Mediator.OpenTelemetry.Shared` README](../Snowberry.Mediator.OpenTelemetry.Shared/README.md).

## Order of Registration

```csharp
public static IServiceRegistry AddSnowberryMediatorOpenTelemetry(
    this IServiceRegistry registry,
    Action<MediatorTelemetryOptions>? configure = null,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
```

- **Call it before `AddSnowberryMediator`.** The decorator claims the `IMediator` registration first; the mediator registration then skips `IMediator` (it uses try-register) and still registers handlers, behaviors and registries. Called after `AddSnowberryMediator`, it throws `InvalidOperationException`.
- **Match the lifetimes.** Pass the same `lifetime` you pass to `AddSnowberryMediator`.
- **Repeated calls are a no-op.**

This is the reverse of the Microsoft DI order, where the decorator is added after the mediator.

## Pitfalls

- **The inner mediator is always `Snowberry.Mediator.Mediator`.** A custom `IMediator` implementation is not wrapped by this decorator; use the Microsoft DI package, or wire `InstrumentedMediator` by hand, to decorate a custom mediator.
- **A changed `SourceName` must be passed to both builders** (`AddSnowberryMediatorInstrumentation(sourceName)`), or the SDK listens to the wrong source and meter.
- **Registering the decorator after the mediator throws.** Keep the OpenTelemetry call first, including in modules that set up the container.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator.OpenTelemetry.Shared`](../Snowberry.Mediator.OpenTelemetry.Shared/README.md) | The decorator, options, conventions and builder extensions. |
| [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) | The mediator registration this package decorates. |
| [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) | The equivalent package for `Microsoft.Extensions.DependencyInjection`. |

# Snowberry.Mediator.Extensions.OpenTelemetry

**OpenTelemetry for [`Snowberry.Mediator`](../../README.md) on `Microsoft.Extensions.DependencyInjection`.** `AddSnowberryMediatorOpenTelemetry` decorates the registered `IMediator` with the `InstrumentedMediator` from [`Snowberry.Mediator.OpenTelemetry.Shared`](../Snowberry.Mediator.OpenTelemetry.Shared/README.md), so every dispatch emits spans and metrics. It targets `net11.0`, `net10.0` and `netstandard2.0`.

For the `Snowberry.DependencyInjection` container use [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) instead.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Registration Rules](#registration-rules)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.Extensions.OpenTelemetry
dotnet add package OpenTelemetry.Extensions.Hosting      # AddOpenTelemetry()
dotnet add package OpenTelemetry.Exporter.Console        # or the exporter you use
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.OpenTelemetry;   // AddSnowberryMediatorInstrumentation

var services = new ServiceCollection();

// 1. Register the mediator.
services.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(MyRequestHandler)];
});

// 2. Decorate it (after AddSnowberryMediator).
services.AddSnowberryMediatorOpenTelemetry(options =>
{
    options.EnablePipelineBehaviorSpans = true;     // optional, off by default
    options.EnableNotificationHandlerSpans = true;  // optional, off by default
});

// 3. Subscribe the OpenTelemetry SDK to the mediator sources and meter.
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSnowberryMediatorInstrumentation().AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddSnowberryMediatorInstrumentation().AddConsoleExporter());
```

The full options table, the emitted activities and metrics, and the enrichment hooks are documented in the [`Snowberry.Mediator.OpenTelemetry.Shared` README](../Snowberry.Mediator.OpenTelemetry.Shared/README.md).

## Registration Rules

```csharp
public static IServiceCollection AddSnowberryMediatorOpenTelemetry(
    this IServiceCollection services,
    Action<MediatorTelemetryOptions>? configure = null)
```

- **Call it after `AddSnowberryMediator`.** It replaces the existing `IMediator` registration with the decorator; called first, it throws `InvalidOperationException` without registering anything, so calling it again after `AddSnowberryMediator` works. The source-generated `AddSnowberryMediator()` works the same way.
- **The decorator keeps the inner mediator's lifetime.** `Singleton` or `Scoped` is recommended; `Transient` works but creates a mediator and a decorator per resolution.
- **Repeated calls are a no-op.** The first call's options win; a later call's `configure` callback does not run.

## Pitfalls

- **Only the first `IMediator` registration is decorated.** Registering another `IMediator` afterwards wins in Microsoft DI and bypasses the decorator.
- **The builder extensions live in `Snowberry.Mediator.OpenTelemetry`.** Without that `using`, `AddSnowberryMediatorInstrumentation()` does not resolve.
- **A changed `SourceName` must be passed to both builders** (`AddSnowberryMediatorInstrumentation(sourceName)`), or the SDK listens to the wrong source and meter.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator.OpenTelemetry.Shared`](../Snowberry.Mediator.OpenTelemetry.Shared/README.md) | The decorator, options, conventions and builder extensions. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) | The mediator registration this package decorates. |
| [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) | The equivalent package for `Snowberry.DependencyInjection`. |

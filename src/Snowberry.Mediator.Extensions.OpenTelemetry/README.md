# Snowberry.Mediator.Extensions.OpenTelemetry

`Microsoft.Extensions.DependencyInjection` integration for `Snowberry.Mediator` OpenTelemetry instrumentation.

This package wires the `InstrumentedMediator` decorator from `Snowberry.Mediator.OpenTelemetry.Shared` into an `IServiceCollection`.

For `Snowberry.DependencyInjection` consumers, use the `Snowberry.Mediator.OpenTelemetry` package instead.

## Usage

```csharp
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;

var services = new ServiceCollection();

services.AddSnowberryMediator(opt =>
{
    opt.RequestHandlerTypes = [typeof(MyRequestHandler)];
    // ...
});

services.AddSnowberryMediatorOpenTelemetry(o =>
{
    o.EnablePipelineBehaviorSpans   = true;  // optional, off by default
    o.EnableNotificationHandlerSpans = true;  // optional, off by default
});

services.AddOpenTelemetry()
    .WithTracing(t => t.AddSnowberryMediatorInstrumentation().AddConsoleExporter())
    .WithMetrics(m => m.AddSnowberryMediatorInstrumentation().AddConsoleExporter());
```

`AddSnowberryMediatorOpenTelemetry` must be called **after** `AddSnowberryMediator`. The extension decorates the existing `IMediator` registration; calling it before throws `InvalidOperationException`. Calling it more than once is a no-op.

The decorator preserves the inner mediator's `ServiceLifetime`. Singleton or Scoped lifetimes are recommended; Transient is supported but allocates a new mediator and decorator per resolution.

See the `Snowberry.Mediator.OpenTelemetry.Shared` package for the full list of emitted activities, metrics, options, and enrichment/filter hooks.

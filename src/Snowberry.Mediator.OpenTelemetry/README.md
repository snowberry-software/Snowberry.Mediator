# Snowberry.Mediator.OpenTelemetry

`Snowberry.DependencyInjection` integration for `Snowberry.Mediator` OpenTelemetry instrumentation.

This package wires the `InstrumentedMediator` decorator from `Snowberry.Mediator.OpenTelemetry.Shared` into an `IServiceRegistry`.

For `Microsoft.Extensions.DependencyInjection` consumers, use the `Snowberry.Mediator.Extensions.OpenTelemetry` package instead.

## Usage

```csharp
using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.OpenTelemetry;

using var container = new ServiceContainer();

// OpenTelemetry must be registered BEFORE AddSnowberryMediator.
container.AddSnowberryMediatorOpenTelemetry(
    o =>
    {
        o.EnablePipelineBehaviorSpans   = true;  // optional, off by default
        o.EnableNotificationHandlerSpans = true;  // optional, off by default
    },
    lifetime: ServiceLifetime.Scoped);

container.AddSnowberryMediator(opt =>
{
    opt.RequestHandlerTypes = [typeof(MyRequestHandler)];
    // ...
}, ServiceLifetime.Scoped);

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();
```

## Order of registration

`AddSnowberryMediatorOpenTelemetry` must be called **before** `AddSnowberryMediator`. Snowberry.DependencyInjection's default registry does not permit re-registering `IMediator` once it has been registered, so the decorator has to win the initial slot. Calling the extension after `AddSnowberryMediator` throws `InvalidOperationException`. Calling it more than once is a no-op.

The `lifetime` argument should match the lifetime that will be passed to `AddSnowberryMediator`.

## Limitations

The decorator's inner mediator is always constructed as a `Snowberry.Mediator.Mediator`. Custom `IMediator` implementations registered against the same registry are not invoked through this decorator. Use the `Snowberry.Mediator.Extensions.OpenTelemetry` package on a `Microsoft.Extensions.DependencyInjection` container if you need to decorate a custom mediator.

See the `Snowberry.Mediator.OpenTelemetry.Shared` package for the full list of emitted activities, metrics, options, and enrichment/filter hooks.

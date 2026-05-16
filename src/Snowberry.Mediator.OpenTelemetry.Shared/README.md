# Snowberry.Mediator.OpenTelemetry.Shared

Core, container-agnostic OpenTelemetry instrumentation for `Snowberry.Mediator`.

This package contains:

- `InstrumentedMediator`: an `IMediator` decorator that emits an `Activity` and metric measurements for every `SendAsync`, `CreateStreamAsync`, and `PublishAsync` dispatch.
- `MediatorInstrumentation`: owns the `ActivitySource`, `Meter`, and the six per-operation `Counter<long>` / `Histogram<double>` instruments.
- `MediatorTelemetryOptions`: configures the instrumentation (source name, tracing/metrics toggles, per-step span opt-ins, enrichment callbacks, filter).
- `TracerProviderBuilderExtensions.AddSnowberryMediatorInstrumentation` and `MeterProviderBuilderExtensions.AddSnowberryMediatorInstrumentation`: subscribe the OpenTelemetry SDK to the activity sources and the meter.

Consumers normally do not reference this package directly. Pick the DI integration package matching the container in use:

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | `Snowberry.Mediator.Extensions.OpenTelemetry` |
| `Snowberry.DependencyInjection` | `Snowberry.Mediator.OpenTelemetry` |

## Emitted telemetry

### Activities

| Source | Activity name | Tags |
| --- | --- | --- |
| `Snowberry.Mediator` | `Mediator.Send {RequestType}` | `snowberry.mediator.request.type`, `snowberry.mediator.response.type`, `snowberry.mediator.operation = send` |
| `Snowberry.Mediator` | `Mediator.Stream {RequestType}` | `snowberry.mediator.request.type`, `snowberry.mediator.response.type`, `snowberry.mediator.operation = stream` |
| `Snowberry.Mediator` | `Mediator.Publish {NotificationType}` | `snowberry.mediator.notification.type`, `snowberry.mediator.operation = publish` |
| `Snowberry.Mediator.Pipeline` (opt-in) | `Mediator.Behavior {BehaviorType}` | `snowberry.mediator.behavior.type`, `snowberry.mediator.request.type` |
| `Snowberry.Mediator.Notification` (opt-in) | `Mediator.Handler {HandlerType}` | `snowberry.mediator.handler.type`, `snowberry.mediator.notification.type` |

All activities use `ActivityKind.Internal`.

### Metrics

Meter name: `Snowberry.Mediator` (configurable via `MediatorTelemetryOptions.SourceName`).

| Instrument | Type | Unit | Tags |
| --- | --- | --- | --- |
| `snowberry.mediator.send.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.send.duration` | `Histogram<double>` | `ms` | `type`, `status` |
| `snowberry.mediator.stream.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.stream.duration` | `Histogram<double>` | `ms` | `type`, `status` |
| `snowberry.mediator.publish.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.publish.duration` | `Histogram<double>` | `ms` | `type`, `status` |

`status` is `"success"` or `"failure"`.

## Per-step spans

Per-pipeline-behavior and per-notification-handler spans are opt-in. Enable them via `MediatorTelemetryOptions`:

```csharp
o.EnablePipelineBehaviorSpans   = true;
o.EnableNotificationHandlerSpans = true;
```

Subscribe the OpenTelemetry SDK to all three sources via the builder extensions:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddSnowberryMediatorInstrumentation()
    .AddConsoleExporter()
    .Build();
```

## Enrichment and filtering

`MediatorTelemetryOptions` exposes enrichment callbacks (`EnrichWithRequest`, `EnrichWithResponse`, `EnrichWithNotification`, `EnrichWithException`) and a `Filter` that short-circuits instrumentation for a given dispatch. Exceptions thrown from enrichment callbacks are recorded as an `Activity` event named `snowberry.mediator.enrichment.failed` and do not propagate to the caller.

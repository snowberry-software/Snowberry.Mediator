# Snowberry.Mediator.OpenTelemetry.Shared

**Container-agnostic OpenTelemetry instrumentation for [`Snowberry.Mediator`](../../README.md).** `InstrumentedMediator` decorates `IMediator` and emits an `Activity` plus count and duration metrics for every `SendAsync`, `CreateStreamAsync` and `PublishAsync`, with opt-in spans per pipeline behavior and per notification handler. It targets `net11.0`, `net10.0` and `netstandard2.0` and depends on [`OpenTelemetry.Api`](https://www.nuget.org/packages/OpenTelemetry.Api).

Applications normally reference the integration for their container, which brings this package in:

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) |
| `Snowberry.DependencyInjection` | [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) |

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Options](#options)
- [How It Works](#how-it-works)
- [Emitted Telemetry](#emitted-telemetry)
- [Per-Step Spans](#per-step-spans)
- [Enrichment and Filtering](#enrichment-and-filtering)
- [Other Containers](#other-containers)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.OpenTelemetry.Shared
```

Exporters and the SDK (`OpenTelemetry`, `OpenTelemetry.Exporter.*`, `OpenTelemetry.Extensions.Hosting`) are not included; add the ones your app uses.

## Quick Start

Decorate the mediator through your container's integration, then subscribe the OpenTelemetry SDK to the mediator sources with the builder extensions from this package:

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.Mediator.OpenTelemetry;   // AddSnowberryMediatorInstrumentation

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSnowberryMediatorInstrumentation()   // Snowberry.Mediator + .Pipeline + .Notification sources
    .AddConsoleExporter()                    // OpenTelemetry.Exporter.Console
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddSnowberryMediatorInstrumentation()   // Snowberry.Mediator meter
    .AddConsoleExporter()
    .Build();
```

## Options

`MediatorTelemetryOptions` (namespace `Snowberry.Mediator.OpenTelemetry`):

| Property | Type | Default | Purpose |
| --- | --- | --- | --- |
| `EnableTracing` | `bool` | `true` | Emit the dispatch `Activity`. |
| `EnableMetrics` | `bool` | `true` | Record the count and duration instruments. |
| `EnablePipelineBehaviorSpans` | `bool` | `false` | Emit one span per pipeline behavior (process-wide, see [Per-Step Spans](#per-step-spans)). |
| `EnableNotificationHandlerSpans` | `bool` | `false` | Emit one span per notification handler (process-wide). |
| `SourceName` | `string` | `"Snowberry.Mediator"` | Name of the dispatch `ActivitySource` and `Meter`. |
| `Filter` | `Func<object, bool>?` | `null` | Return `false` to skip instrumentation for a request or notification. |
| `EnrichWithRequest` | `Action<Activity, object>?` | `null` | Add tags from the request (`Send`, `Stream`). |
| `EnrichWithResponse` | `Action<Activity, object, object>?` | `null` | Add tags from the response (`Send` only). |
| `EnrichWithNotification` | `Action<Activity, object>?` | `null` | Add tags from the notification (`Publish`). |
| `EnrichWithException` | `Action<Activity, object, Exception>?` | `null` | Add tags or events for a failed dispatch. |

The options are read when the decorator is constructed.

## How It Works

```text
caller -> InstrumentedMediator ----------------------------> inner IMediator
             |  no tracer listening and no meter listener:    (forwarded as-is)
             |  Filter(message) == false:                     (forwarded as-is)
             |
             +-> start Activity "Mediator.Send {RequestType}"
             +-> inner.SendAsync(...)
             +-> status Ok / Error, count + duration metrics, stop Activity
```

The instrumented path is taken when tracing is enabled and the `ActivitySource` has a listener, or when metrics are enabled and an instrument is being listened to. Otherwise the call is forwarded to the inner mediator directly.

## Emitted Telemetry

### Activities

| Source | Activity name | Tags |
| --- | --- | --- |
| `Snowberry.Mediator` | `Mediator.Send {RequestType}` | `snowberry.mediator.request.type`, `snowberry.mediator.response.type`, `snowberry.mediator.operation = send` |
| `Snowberry.Mediator` | `Mediator.Stream {RequestType}` | `snowberry.mediator.request.type`, `snowberry.mediator.response.type`, `snowberry.mediator.operation = stream` |
| `Snowberry.Mediator` | `Mediator.Publish {NotificationType}` | `snowberry.mediator.notification.type`, `snowberry.mediator.operation = publish` |
| `Snowberry.Mediator.Pipeline` (opt-in) | `Mediator.Behavior {BehaviorType}` | `snowberry.mediator.behavior.type`, `snowberry.mediator.request.type` |
| `Snowberry.Mediator.Notification` (opt-in) | `Mediator.Handler {HandlerType}` | `snowberry.mediator.handler.type`, `snowberry.mediator.notification.type` |

- All activities use `ActivityKind.Internal`. The status is `Ok` on success and `Error` (with the exception message) on failure; no exception event is recorded, so add one in `EnrichWithException` if you need it.
- Type names are `Type.FullName` (for example `MyApp.Orders.PlaceOrder`), so types that share a simple name across namespaces are not conflated. For closed generic types, such as open-generic behaviors closed at runtime, the full name includes assembly-qualified type arguments.
- Stream activities start on the first `MoveNextAsync` and cover the whole enumeration, including time spent in the consumer.

### Metrics

Meter name: `SourceName` (default `Snowberry.Mediator`).

| Instrument | Type | Unit | Tags |
| --- | --- | --- | --- |
| `snowberry.mediator.send.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.send.duration` | `Histogram<double>` | `ms` | `type`, `status` |
| `snowberry.mediator.stream.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.stream.duration` | `Histogram<double>` | `ms` | `type`, `status` |
| `snowberry.mediator.publish.count` | `Counter<long>` | | `type`, `status` |
| `snowberry.mediator.publish.duration` | `Histogram<double>` | `ms` | `type`, `status` |

`status` is `success` or `failure`. A stream the consumer stops enumerating early counts as `failure`.

### Instrumentation scope

Every `ActivitySource` and the `Meter` carry the package version as their version, exported as the OpenTelemetry instrumentation scope version. It is available as `MediatorTelemetryConventions.c_InstrumentationVersion`. All names and tag keys are public constants on `MediatorTelemetryConventions` (`ActivityNames`, `Tags`, `Operations`, `Status`, `Instruments`, `HookNames`).

## Per-Step Spans

```csharp
options.EnablePipelineBehaviorSpans = true;     // Mediator.Behavior spans, request and stream pipelines
options.EnableNotificationHandlerSpans = true;  // Mediator.Handler spans
```

- The flags are **process-wide and cannot be turned off again**: enabling them calls `MediatorDiagnostics.EnablePipelineSpans()` / `EnableNotificationSpans()` in the core package when the decorator is registered. You can call those methods directly to get per-step spans without the decorator.
- Per-step spans come from the fixed `Snowberry.Mediator.Pipeline` and `Snowberry.Mediator.Notification` sources and ignore `EnableTracing` and `Filter`.
- `AddSnowberryMediatorInstrumentation()` on the tracer builder subscribes both sources.

## Enrichment and Filtering

- **Enrichment callbacks never break a dispatch.** An exception thrown from an `EnrichWith*` callback is caught and recorded as an `Activity` event named `snowberry.mediator.enrichment.failed` with the tags `exception.type`, `exception.message` and `snowberry.mediator.hook.name`.
- **Callbacks run only when an `Activity` exists**, so not in metrics-only mode and not when the span is sampled out.
- **`EnrichWithException`** runs for failed `SendAsync`, `PublishAsync` and `CreateStreamAsync` calls, including a stream whose inner mediator fails before enumeration starts (for example `HandlerNotFoundException`). It does not run when a consumer merely stops enumerating early; the span is still marked `Error`.
- **`Filter` is not guarded.** It runs before any `Activity` exists, so an exception from `Filter` propagates out of the dispatch call. Keep it total (non-throwing).

## Other Containers

The building blocks are public, so any container can be wired by hand:

```csharp
var options = new MediatorTelemetryOptions();
var instrumentation = new MediatorInstrumentation(options);      // owns ActivitySource, Meter, instruments; IDisposable
IMediator instrumented = new InstrumentedMediator(innerMediator, instrumentation, options);

MediatorDiagnostics.EnablePipelineSpans();                      // per-step spans are switched on separately
```

Register `MediatorInstrumentation` as a singleton and dispose it with the container. The `EnablePipelineBehaviorSpans` / `EnableNotificationHandlerSpans` options only take effect through the DI integrations; call `MediatorDiagnostics` directly when wiring by hand.

## Pitfalls

- **A custom `SourceName` must be passed to the builders.** Use `AddSnowberryMediatorInstrumentation(sourceName)` on the tracer builder and `AddSnowberryMediatorInstrumentation(meterName)` on the meter builder; the parameterless overloads subscribe to `Snowberry.Mediator` and would miss everything.
- **A meter listener alone activates the instrumented path.** Subscribing a `MeterProvider` makes every dispatch go through the decorator even without tracing; set `EnableMetrics = false` if you only want spans.
- **Per-step span flags are one-way.** Once enabled they stay on for the process, including other mediator instances.
- **Keep `Filter` non-throwing.** Its exceptions reach the caller.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation and `MediatorDiagnostics`. |
| [`Snowberry.Mediator.Extensions.OpenTelemetry`](../Snowberry.Mediator.Extensions.OpenTelemetry/README.md) | Decorator wiring for `Microsoft.Extensions.DependencyInjection`. |
| [`Snowberry.Mediator.OpenTelemetry`](../Snowberry.Mediator.OpenTelemetry/README.md) | Decorator wiring for `Snowberry.DependencyInjection`. |

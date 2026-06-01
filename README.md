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
  - `IPipelineBehavior<TRequest, TResponse>` / `IPipelineContinuation<TRequest, TResponse>`
  - `IStreamPipelineBehavior<TRequest, TResponse>` / `IStreamPipelineContinuation<TRequest, TResponse>`
- Assembly scanning helper to discover handlers, pipeline behaviors and notification handlers (`MediatorAssemblyHelper`).
- Global registries for pipeline and notification handlers used at runtime by `Mediator`.
- Zero per-call allocation through the mediator's own dispatch code (see [Performance](#performance)). `SendAsync` / `PublishAsync` return `ValueTask` / `ValueTask<T>` and never allocate a state machine inside the library - any per-call allocation that remains comes from your own handlers if they use `async`/`await` and suspend.
- AOT-compatible: `Snowberry.Mediator`, `Snowberry.Mediator.Abstractions` and the DI helpers ship with `IsAotCompatible=true` on `net9.0+`. Assembly-scanning entry points are gated with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`; the explicit-registration entry points are AOT-safe.
- OpenTelemetry tracing and metrics through an opt-in `IMediator` decorator, gated on `ActivitySource.HasListeners()` so it costs nothing when no listener is attached (see [OpenTelemetry](#opentelemetry)).
- Two first-party dependency-injection integrations: `Microsoft.Extensions.DependencyInjection` and the sibling `Snowberry.DependencyInjection` container.

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

### Snowberry.DependencyInjection

`Snowberry.Mediator.DependencyInjection` integrates with the sibling [`Snowberry.DependencyInjection`](https://github.com/snowberry-software/Snowberry.DependencyInjection) container. It exposes `AddSnowberryMediator` and `AppendSnowberryMediator` on `IServiceRegistry`, taking the same `MediatorOptions` as the Microsoft container. The registry must also be resolvable as an `IServiceProvider` so the mediator can resolve handlers at dispatch time.

Example: register by scanning the current assembly and enable pipeline/notification scanning:

```csharp
using System.Reflection;
using Snowberry.DependencyInjection;                   // ServiceContainer
using Snowberry.DependencyInjection.Abstractions;      // ServiceLifetime
using Snowberry.DependencyInjection.Abstractions.Extensions; // GetRequiredService
using Snowberry.Mediator.DependencyInjection;          // AddSnowberryMediator

using var registry = new ServiceContainer();

registry.AddSnowberryMediator(options =>
{
    options.Assemblies = new List<Assembly> { Assembly.GetExecutingAssembly() };
    options.ScanNotificationHandlers = true;
    options.ScanPipelineBehaviors = true;
}, ServiceLifetime.Scoped);

var mediator = registry.GetRequiredService<Snowberry.Mediator.Abstractions.IMediator>();
```

To extend a registry that already has the mediator (for example when loading plugins), call `AppendSnowberryMediator`. Calling `AddSnowberryMediator` a second time throws, so a double registration cannot silently orphan handlers into a discarded registry.

The source generator targets this container too: when `Snowberry.Mediator.DependencyInjection` is referenced, the generated `AddSnowberryMediator()` (with an `append` overload) is emitted as an extension on `IServiceRegistry`, with the same zero-reflection guarantees described below.

### Source generator (zero-reflection, NativeAOT-friendly)

The `Snowberry.Mediator.SourceGenerator` package discovers handlers, behaviors and notification handlers **at compile time** (across your project and its referenced assemblies) and emits the registration with literal closed generics, eliminating all runtime reflection (`Assembly.GetTypes()`, `Type.GetInterfaces()`, `MakeGenericType`). It is the recommended setup for trimmed / NativeAOT apps.

1. Reference `Snowberry.Mediator.SourceGenerator` alongside `Snowberry.Mediator` and a DI integration package.
2. Add the opt-in attribute to your composition-root project:

   ```csharp
   [assembly: Snowberry.Mediator.SnowberryMediator]
   ```

3. Call the generated registration with no `options.Assemblies` and no hand-listed handler types:

   ```csharp
   using Microsoft.Extensions.DependencyInjection;

   var services = new ServiceCollection();
   services.AddSnowberryMediator();                         // default Scoped lifetime
   // services.AddSnowberryMediator(ServiceLifetime.Singleton);

   var mediator = services.BuildServiceProvider()
       .GetRequiredService<Snowberry.Mediator.Abstractions.IMediator>();
   ```

Handlers in referenced assemblies are discovered as long as the composition-root project can access the type (public, or `internal` exposed via `[InternalsVisibleTo]`). `[PipelineOverwritePriority]` ordering, open-generic behaviors and open-generic notification handlers are fully supported, with byte-identical dispatch semantics to the reflection path. Only the *startup* registration changes, so the dispatch numbers below are unaffected. The generated path executes no dynamic code, so it is clean under `PublishAot`/trimming. See the package README for the diagnostics table and the runnable AOT sample under `samples/`.

To restrict which referenced assemblies are scanned, set `[assembly: SnowberryMediator(ScanReferencedAssemblies = false)]` and name each one to include with `[assembly: SnowberryMediatorAssembly(typeof(AnyTypeInThatAssembly))]` (the current assembly is always scanned). To give a single handler a non-default lifetime, register it before `AddSnowberryMediator()`; the generated registrations use `TryAdd`, so a pre-registered handler keeps your lifetime. Both are documented in the package README.

## OpenTelemetry

`Snowberry.Mediator` ships first-class OpenTelemetry tracing and metrics through an `IMediator` decorator (`InstrumentedMediator`). It emits an `Activity` and metric measurements for every `SendAsync`, `CreateStreamAsync` and `PublishAsync` dispatch, with opt-in per-pipeline-behavior and per-notification-handler spans. The decorator is gated on `ActivitySource.HasListeners()`, so with no listener attached the dispatch path stays allocation-free.

Pick the integration package that matches your container:

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | `Snowberry.Mediator.Extensions.OpenTelemetry` |
| `Snowberry.DependencyInjection` | `Snowberry.Mediator.OpenTelemetry` |

### Microsoft Dependency Injection

Register the mediator first, then decorate it. Subscribe the OpenTelemetry SDK to the mediator sources with `AddSnowberryMediatorInstrumentation()` on the tracer and meter builders:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

services.AddSnowberryMediator(/* ... */);

services.AddSnowberryMediatorOpenTelemetry(options =>
{
    options.EnablePipelineBehaviorSpans = true;
    options.EnableNotificationHandlerSpans = true;
});

services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSnowberryMediatorInstrumentation())
    .WithMetrics(metrics => metrics.AddSnowberryMediatorInstrumentation());
```

`AddSnowberryMediatorOpenTelemetry` must run after `AddSnowberryMediator`, because it swaps the existing `IMediator` registration for the decorator.

### Snowberry.DependencyInjection

The order is reversed here: call `AddSnowberryMediatorOpenTelemetry` before `AddSnowberryMediator`, because the registry does not permit re-registering `IMediator` once it exists. The decorator claims the `IMediator` slot first, and the later mediator registration becomes a no-op.

```csharp
using Snowberry.DependencyInjection;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.OpenTelemetry;

using var registry = new ServiceContainer();

registry.AddSnowberryMediatorOpenTelemetry(options =>
{
    options.EnablePipelineBehaviorSpans = true;
    options.EnableNotificationHandlerSpans = true;
});

registry.AddSnowberryMediator(/* ... */);
```

Subscribe the SDK with the same `AddSnowberryMediatorInstrumentation()` builder extensions shown above; they live in the `Snowberry.Mediator.OpenTelemetry` namespace, reachable from both packages.

### Emitted telemetry

Top-level activities come from the `Snowberry.Mediator` source. The opt-in per-step spans come from `Snowberry.Mediator.Pipeline` and `Snowberry.Mediator.Notification`.

| Activity | Tags |
| --- | --- |
| `Mediator.Send {RequestType}` | request type, response type, `operation = send` |
| `Mediator.Stream {RequestType}` | request type, response type, `operation = stream` |
| `Mediator.Publish {NotificationType}` | notification type, `operation = publish` |
| `Mediator.Behavior {BehaviorType}` (opt-in) | behavior type, request type |
| `Mediator.Handler {HandlerType}` (opt-in) | handler type, notification type |

Six metric instruments are emitted under the `Snowberry.Mediator` meter: a `Counter<long>` and a `Histogram<double>` (`ms`) for each of send, stream and publish, tagged with `type` and `status` (`success` or `failure`). Activity names and the `*.type` tags use the fully-qualified type name, so types that share a simple name across namespaces are never conflated into one span or metric series.

### Enrichment and filtering

`MediatorTelemetryOptions` exposes enrichment callbacks (`EnrichWithRequest`, `EnrichWithResponse`, `EnrichWithNotification`, `EnrichWithException`) and a `Filter` that short-circuits instrumentation for a dispatch. Exceptions thrown from enrichment callbacks are recorded as an `Activity` event named `snowberry.mediator.enrichment.failed` and do not propagate to the caller. `Filter` is the exception: it runs before any `Activity` exists, so a throwing `Filter` propagates out of the dispatch call. Keep it total (non-throwing).

A runnable, end-to-end example wired into the .NET Aspire dashboard lives under [`samples/`](samples/README.md). The full conventions, including every tag key and the per-step opt-in details, are in the [package README](src/Snowberry.Mediator.OpenTelemetry.Shared/README.md).

## Writing a pipeline behavior

A pipeline behavior implements `IPipelineBehavior<TRequest, TResponse>`. Its `HandleAsync` method receives a struct continuation that you invoke to call the next behavior in the chain (or, at the end of the chain, the terminal request handler):

```csharp
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync<TNext>(
        TRequest request,
        TNext next,
        CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
    {
        // pre-handler logic - runs before the next behavior / terminal handler

        var response = await next.InvokeAsync(request, cancellationToken);

        // post-handler logic - runs after the next behavior / terminal handler returns
        return response;
    }
}
```

The `TNext : struct, IPipelineContinuation<,>` constraint lets the JIT specialize the method per continuation type and devirtualize `next.InvokeAsync(...)` to a direct call. Combined with the static-generic walker the library uses internally, this keeps the synchronous dispatch path allocation-free regardless of chain length.

Stream pipeline behaviors (`IStreamPipelineBehavior<TRequest, TResponse>`) follow the same shape: their `HandleAsync<TNext>(request, next, ct)` returns `IAsyncEnumerable<TResponse>` and `next` is constrained as `where TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>`. One mental model for both request and stream pipelines.

## Pipeline behavior ordering and priority

Pipeline behaviors are executed as a chain. The order matters because each behavior receives a continuation that it invokes to advance to the next behavior - the first behavior in dispatch order wraps every subsequent behavior and the terminal handler.

Ordering rules:

- When you explicitly provide `PipelineBehaviorTypes` (or `StreamPipelineBehaviorTypes`), the order of types in the list is preserved and used as the base ordering.
- When pipeline behaviors are discovered via scanning, or when combining scanned and explicit lists, the framework uses the `PipelineOverwritePriorityAttribute` applied to behavior types to determine priority.
- `PipelineOverwritePriorityAttribute` contains an integer `Priority` property. Higher values indicate higher priority - behaviors with a higher `Priority` value are executed earlier in the chain (and therefore wrap behaviors with lower priorities).

Practical guidance:

- If you need a behavior to run first (for example, logging or diagnostics that should wrap everything), give it a higher `Priority`.
- If you mix scanned behaviors and an explicit list, set priorities on scanned implementations when their relative position matters, or prefer explicit ordering for predictable placement.

## Performance

`Snowberry.Mediator` is designed for hot-path dispatch with zero per-call allocation from the library itself. The dispatch code (`Mediator.SendAsync`, the pipeline walker, the registries) is not `async` anywhere on the hot path - it returns whatever `ValueTask` / `ValueTask<T>` your handler chain produces, without ever wrapping it in its own state machine. When your handlers and behaviors also return synchronously-completed `ValueTask`s, the entire Send/Publish is 0 B end-to-end. When your `async` handler actually suspends at an `await`, the runtime boxes that handler's state machine - that allocation comes from your code, not from the library.

Representative numbers on .NET 10 (`BenchmarkDotNet 0.14.0`, `MemoryDiagnoser`):

| Scenario                                  |    Latency | Allocated |
| ----------------------------------------- | ---------: | --------: |
| `Send_NoPipeline`                         |   ~10.4 ns |       0 B |
| `Send_Specific1`                          |   ~34.8 ns |       0 B |
| `Send_Specific3`                          |   ~60.2 ns |       0 B |
| `Send_Specific10`                         |  ~184.5 ns |       0 B |
| `Send_OpenGeneric1`                       |   ~34.1 ns |       0 B |
| `Send_OpenGeneric3`                       |   ~60.1 ns |       0 B |
| `Send_Mixed4` (2 specific + 2 generic)    |   ~86.4 ns |       0 B |
| `Publish_Specific3` (3 sync handlers)     |   ~39.8 ns |       0 B |
| `Publish_OpenGeneric3`                    |   ~44.3 ns |       0 B |
| `Publish_Mixed3`                          |   ~46.2 ns |       0 B |
| `Stream_NoPipeline_Enumerate10`           |  ~134.4 ns |     104 B¹ |
| `Stream_Specific1_Enumerate10`            |  ~287.6 ns |     352 B¹ |
| `Stream_Specific3_Enumerate10`            |  ~580.7 ns |     848 B¹ |
| `Stream_Specific10_Enumerate10`           | ~1547.2 ns |    2584 B¹ |
| `Stream_OpenGeneric3_Enumerate10`         |  ~617.7 ns |     848 B¹ |
| `Send_Specific1_Async` (awaiting handler) |  ~466.6 ns |     264 B² |

¹ The stream allocations are the compiler-emitted `async IAsyncEnumerable<T>` enumerator state machines in user code - one per behavior plus the terminal handler.
² The async-Send 264 B is the user handler's `async ValueTask<T>` state-machine box. Opt into the pooled builder per method to recycle these:

```csharp
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
public async ValueTask<MyResponse> HandleAsync(MyRequest request, CancellationToken cancellationToken = default)
{
    // ... uses await ...
}
```

How the library reaches these numbers:

- Pipeline and notification registries are resolved once in the `Mediator` constructor, not on every Send.
- Behavior registrations are sorted once during `Build()` and snapshotted into a `FrozenDictionary<Type, T[]>`; the per-Send dispatch path reads the pre-sorted arrays directly.
- Open-generic behavior types are closed once per `(TRequest, TResponse)` pair via `MakeGenericType` and cached - the cost is paid on the first Send for each pair, never repeated. Notification open-generic handler types are cached the same way per notification type.
- The synchronous request pipeline uses a `readonly struct` walker that implements `IPipelineContinuation<TRequest, TResponse>` and is passed by value through the chain. The JIT specializes each behavior's `HandleAsync<TNext>` per walker type, so chain steps are direct calls with no delegate allocation.
- A static-generic fast cache (`PipelineFastCache<TRequest, TResponse>`) holds the closed behavior-type array for a hot pair. Hot-path lookup is a single `Volatile.Read` of an immutable carrier object plus an owner/generation check; multi-registry scenarios (e.g. test suites) fall back to a per-instance `ConcurrentDictionary` with no correctness impact.
- The stream pipeline (`IStreamPipelineBehavior` / `IStreamPipelineContinuation`) uses the same walker and `StreamPipelineFastCache` pattern. The remaining 104+ B per stream behavior shown in the table is the compiler-generated `async IAsyncEnumerable<T>` state machine in **your** code, allocated once per `CreateStreamAsync` call per behavior; the mediator's own per-call overhead is zero.

Benchmarks live in the `Snowberry.Mediator.Benchmarks` project.
# Snowberry.Mediator

[![License](https://img.shields.io/github/license/snowberry-software/Snowberry.Mediator)](https://github.com/snowberry-software/Snowberry.Mediator/blob/master/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/Snowberry.Mediator.svg?logo=nuget)](https://www.nuget.org/packages/Snowberry.Mediator/)

**A lightweight, allocation-conscious mediator for .NET.** `Snowberry.Mediator` routes requests to a single handler, streams results from a stream handler and fans notifications out to every subscriber, with optional pipeline behaviors wrapped around requests and streams. It targets `net11.0`, `net10.0` and `netstandard2.0`.

Handlers are registered through a dependency-injection integration ([`Microsoft.Extensions.DependencyInjection`](src/Snowberry.Mediator.Extensions.DependencyInjection/README.md) or [`Snowberry.DependencyInjection`](src/Snowberry.Mediator.DependencyInjection/README.md)), either by assembly scanning, by explicit type lists, or at compile time with the [source generator](src/Snowberry.Mediator.SourceGenerator/README.md) for trimmed and Native AOT apps. OpenTelemetry tracing and metrics are available as an opt-in decorator.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Top Features at a Glance](#top-features-at-a-glance)
- [How It Works](#how-it-works)
- [Messages and Handlers](#messages-and-handlers)
- [Registration](#registration)
- [Pipeline Behaviors](#pipeline-behaviors)
- [Notifications](#notifications)
- [OpenTelemetry](#opentelemetry)
- [Native AOT and Trimming](#native-aot-and-trimming)
- [Performance](#performance)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

Install the DI integration for your container; it brings in `Snowberry.Mediator` and `Snowberry.Mediator.Abstractions`:

```bash
# Microsoft.Extensions.DependencyInjection
dotnet add package Snowberry.Mediator.Extensions.DependencyInjection

# or Snowberry.DependencyInjection
dotnet add package Snowberry.Mediator.DependencyInjection
```

Add the source generator for reflection-free registration (recommended for trimmed and Native AOT apps):

```bash
dotnet add package Snowberry.Mediator.SourceGenerator
```

## Quick Start

1. Define a request and its handler.
2. Register the mediator and the handler.
3. Resolve `IMediator` and send.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Extensions.DependencyInjection;

// 2. Register the mediator and the handler.
var services = new ServiceCollection();
services.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(PingHandler)];
});

// 3. Resolve IMediator (Scoped by default) and send.
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

string reply = await mediator.SendAsync(new Ping("World"));
Console.WriteLine(reply); // Pong, World

// 1. Define a request and its handler.
public sealed record Ping(string Name) : IRequest<Ping, string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> HandleAsync(Ping request, CancellationToken cancellationToken = default)
        => new($"Pong, {request.Name}");
}
```

`ServiceCollection` and `BuildServiceProvider` come from the `Microsoft.Extensions.DependencyInjection` package; a generic host already provides them.

## Top Features at a Glance

- **Requests, streams and notifications.** `SendAsync` returns `ValueTask<TResponse>`, `CreateStreamAsync` returns `IAsyncEnumerable<TResponse>` and `PublishAsync` runs every handler of a notification.
- **Struct-based pipeline behaviors.** Behaviors receive the next step as a `struct` continuation, so the JIT specializes each chain step into a direct call with no delegate allocation.
- **Zero library allocation on the synchronous path.** When handlers and behaviors complete synchronously, `Send` and `Publish` allocate 0 B end to end (see [Performance](#performance)).
- **Explicit ordering.** `[PipelineOverwritePriority]` moves a behavior ahead of the registration order (see [Ordering and Priority](#ordering-and-priority)).
- **Three registration styles.** Assembly scanning, explicit type lists, or compile-time discovery with the source generator.
- **Two first-party DI integrations.** `Microsoft.Extensions.DependencyInjection` and [`Snowberry.DependencyInjection`](https://github.com/snowberry-software/Snowberry.DependencyInjection).
- **Opt-in OpenTelemetry.** An `IMediator` decorator emits spans and metrics, and forwards straight to the inner mediator when nothing is listening.
- **Native AOT ready.** The source-generated registration path runs no assembly scanning and no reachable dynamic code.

## How It Works

The integration registers `IMediator`, your handlers and, when you have behaviors or notification handlers, a global pipeline registry and a global notification registry. `Mediator` resolves the registries once in its constructor and resolves handlers from the `IServiceProvider` on every dispatch.

```text
SendAsync(request)
  -> PipelineFastCache<TRequest, TResponse>    closed behavior types for this pair (cached)
  -> PipelineWalker (readonly struct)          index into the behavior array
       -> Behavior[0].HandleAsync(request, next)
            -> Behavior[1].HandleAsync(request, next)
                 -> IRequestHandler<TRequest, TResponse>.HandleAsync(request)

CreateStreamAsync(request)   same shape with IStreamPipelineBehavior / IStreamRequestHandler
PublishAsync(notification)   every INotificationHandler<T>, one after another
```

Open-generic behaviors are closed once per `(TRequest, TResponse)` pair and cached. When no pipeline registry is registered (or it is empty), `SendAsync` calls the request handler directly.

## Messages and Handlers

| Message | Marker interface | Handler | Mediator call |
| --- | --- | --- | --- |
| Request | `IRequest<TRequest, TResponse>` | `IRequestHandler<TRequest, TResponse>` | `ValueTask<TResponse> SendAsync(request, ct)` |
| Stream request | `IStreamRequest<TRequest, TResponse>` | `IStreamRequestHandler<TRequest, TResponse>` | `IAsyncEnumerable<TResponse> CreateStreamAsync(request, ct)` |
| Notification | `INotification` | `INotificationHandler<TNotification>` | `ValueTask PublishAsync(notification, ct)` |

- Requests name themselves as the first type argument (`record Ping : IRequest<Ping, string>`), which lets the compiler infer both type arguments of `SendAsync`.
- Use `Empty` as the response type for requests with no result; `Empty.ValueTask` is a completed `ValueTask<Empty>`.
- Each request type needs exactly one handler. A missing handler throws `HandlerNotFoundException` from `SendAsync` / `CreateStreamAsync`.
- Inject `IMediator`. Only `IMediator` is registered in the container; `IMediatorSender` and `IMediatorPublisher` are the interfaces it combines.

The interfaces live in `Snowberry.Mediator.Abstractions` (`IMediator`, `Empty`), `.Messages`, `.Handler`, `.Pipeline`, `.Attributes` and `.Exceptions`. See the [Abstractions README](src/Snowberry.Mediator.Abstractions/README.md) for the full list.

## Registration

Both integrations take the same `MediatorOptions` (namespace `Snowberry.Mediator`):

| Property | Type | Default | Purpose |
| --- | --- | --- | --- |
| `Assemblies` | `List<Assembly>?` | `null` | Assemblies to scan. |
| `RequestHandlerTypes` | `List<Type>?` | `null` | Explicit request handlers. |
| `StreamRequestHandlerTypes` | `List<Type>?` | `null` | Explicit stream handlers. |
| `NotificationHandlerTypes` | `List<Type>?` | `null` | Explicit notification handlers (open generics allowed). |
| `PipelineBehaviorTypes` | `List<Type>?` | `null` | Explicit pipeline behaviors, in order (open generics allowed). |
| `StreamPipelineBehaviorTypes` | `List<Type>?` | `null` | Explicit stream pipeline behaviors, in order. |
| `ScanNotificationHandlers` | `bool` | `false` | Also scan `Assemblies` for notification handlers. |
| `ScanPipelineBehaviors` | `bool` | `false` | Also scan `Assemblies` for pipeline behaviors. |
| `ScanStreamPipelineBehaviors` | `bool` | `false` | Also scan `Assemblies` for stream pipeline behaviors. |
| `RegisterRequestHandlers`, `RegisterStreamRequestHandlers`, `RegisterNotificationHandlers`, `RegisterPipelineBehaviors`, `RegisterStreamPipelineBehaviors` | `bool` | `true` | Switch a whole category off. |

Request and stream handlers are scanned whenever `Assemblies` is set; behaviors and notification handlers are scanned only when their `Scan*` flag is also `true`.

### Microsoft.Extensions.DependencyInjection

```csharp
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;

// Assembly scanning
services.AddSnowberryMediator(options =>
{
    options.Assemblies = [Assembly.GetExecutingAssembly()];
    options.ScanNotificationHandlers = true;
    options.ScanPipelineBehaviors = true;
});

// Explicit type lists
services.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(PlaceOrderHandler)];
    options.PipelineBehaviorTypes = [typeof(LoggingBehavior<,>), typeof(ValidationBehavior<,>)];
    options.NotificationHandlerTypes = [typeof(OrderPlacedHandler)];
}, ServiceLifetime.Scoped);
```

The signature is `AddSnowberryMediator(Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = Scoped, bool append = false)`. The lifetime applies to `IMediator` and every handler and behavior.

### Snowberry.DependencyInjection

```csharp
using Snowberry.DependencyInjection;                          // ServiceContainer
using Snowberry.DependencyInjection.Abstractions;             // ServiceLifetime
using Snowberry.DependencyInjection.Abstractions.Extensions;  // GetRequiredService
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;                 // AddSnowberryMediator

using var container = new ServiceContainer();

container.AddSnowberryMediator(options =>
{
    options.RequestHandlerTypes = [typeof(PlaceOrderHandler)];
    options.PipelineBehaviorTypes = [typeof(LoggingBehavior<,>)];
}, ServiceLifetime.Scoped);

var mediator = container.GetRequiredService<IMediator>();
```

This integration offers `AppendSnowberryMediator` instead of an `append` parameter.

### Adding handlers later

Registrations use try-add semantics: an existing `IMediator` or handler is never replaced. A second `AddSnowberryMediator` call that would register behaviors or notification handlers while a registry already exists throws `InvalidOperationException`, so modules cannot silently create a second, unused registry. To contribute more handlers to an existing setup (plugins, feature modules), append instead:

```csharp
services.AddSnowberryMediator(options => { /* ... */ }, append: true);  // Microsoft DI
container.AppendSnowberryMediator(options => { /* ... */ });           // Snowberry DI
```

Appending adds the new behaviors and notification handlers to the existing registries.

### Source generator

The [`Snowberry.Mediator.SourceGenerator`](src/Snowberry.Mediator.SourceGenerator/README.md) package discovers handlers, behaviors and notification handlers at compile time, across your project and its referenced assemblies, and emits the registration with literal closed generic types:

```csharp
[assembly: Snowberry.Mediator.SnowberryMediator]

services.AddSnowberryMediator();                            // default Scoped lifetime
services.AddSnowberryMediator(ServiceLifetime.Singleton);
services.AddSnowberryMediator(append: true);                // contribute to an existing setup
```

The generated `AddSnowberryMediator(ServiceLifetime lifetime = Scoped, bool append = false)` is emitted for `IServiceCollection` (namespace `Microsoft.Extensions.DependencyInjection`) and for `IServiceRegistry` (namespace `Snowberry.Mediator.DependencyInjection`), depending on which integration you reference. Dispatch semantics, including `[PipelineOverwritePriority]` ordering, are identical to the reflection path; only startup registration changes.

## Pipeline Behaviors

### Writing a behavior

A pipeline behavior implements `IPipelineBehavior<TRequest, TResponse>`. `HandleAsync` receives a struct continuation; invoke it to call the next behavior or, at the end of the chain, the request handler:

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
        // runs before the next behavior / the handler
        var response = await next.InvokeAsync(request, cancellationToken);
        // runs after the next behavior / the handler returns
        return response;
    }
}
```

The `TNext : struct` constraint lets the JIT specialize the method per continuation type and devirtualize `next.InvokeAsync(...)` into a direct call. A behavior that does not need `await` can return `next.InvokeAsync(...)` directly and stay allocation-free.

Stream behaviors (`IStreamPipelineBehavior<TRequest, TResponse>`) have the same shape: `HandleAsync<TNext>(request, next, ct)` returns `IAsyncEnumerable<TResponse>` with `TNext : struct, IStreamPipelineContinuation<TRequest, TResponse>`.

### Ordering and priority

The first behavior in dispatch order wraps every later behavior and the handler.

- **Without the attribute,** behaviors run in registration order. Explicit `PipelineBehaviorTypes` / `StreamPipelineBehaviorTypes` keep their list order; when scanning and an explicit list are combined, scanned behaviors are registered first.
- **`[PipelineOverwritePriority(Priority = n)]`** with a positive `n` runs the behavior ahead of every unattributed behavior. Among attributed behaviors, a higher `Priority` runs earlier. The attribute applies to explicitly listed behaviors too, overriding their list position.
- **Zero and negative priorities** are compared against registration positions, so they interleave with unattributed behaviors rather than running last. Use positive values only.

```csharp
[PipelineOverwritePriority(Priority = 100)]   // outermost: wraps everything else
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { /* ... */ }
```

## Notifications

`PublishAsync` resolves every handler for the notification and awaits them one after another: open-generic handlers first, then specific ones. The first exception stops the remaining handlers and propagates to the caller.

- With no notification handlers registered at all, `PublishAsync` throws `NotificationHandlerNotFoundException`.
- When handlers exist but none for this notification type (or the container cannot resolve one), it throws `NotificationHandlerNotResolvedException`.

## OpenTelemetry

An `IMediator` decorator (`InstrumentedMediator`) emits an `Activity` and metric measurements for every `SendAsync`, `CreateStreamAsync` and `PublishAsync`, with opt-in spans per pipeline behavior and per notification handler. When no tracer listens to its `ActivitySource` and no meter listener is subscribed, it forwards directly to the inner mediator.

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | [`Snowberry.Mediator.Extensions.OpenTelemetry`](src/Snowberry.Mediator.Extensions.OpenTelemetry/README.md) |
| `Snowberry.DependencyInjection` | [`Snowberry.Mediator.OpenTelemetry`](src/Snowberry.Mediator.OpenTelemetry/README.md) |

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.OpenTelemetry;               // AddSnowberryMediatorInstrumentation

services.AddSnowberryMediator(options => { /* ... */ });
services.AddSnowberryMediatorOpenTelemetry(options =>  // after AddSnowberryMediator
{
    options.EnablePipelineBehaviorSpans = true;
    options.EnableNotificationHandlerSpans = true;
});

services.AddOpenTelemetry()                           // OpenTelemetry.Extensions.Hosting
    .WithTracing(tracing => tracing.AddSnowberryMediatorInstrumentation())
    .WithMetrics(metrics => metrics.AddSnowberryMediatorInstrumentation());
```

On Microsoft DI the decorator is added **after** `AddSnowberryMediator`; on Snowberry DI it is added **before**. The activity names, tags, metric instruments, options and enrichment hooks are documented in the [`Snowberry.Mediator.OpenTelemetry.Shared` README](src/Snowberry.Mediator.OpenTelemetry.Shared/README.md). A runnable sample wired into the .NET Aspire dashboard lives under [`samples/`](samples/README.md).

## Native AOT and Trimming

- `Snowberry.Mediator`, `Snowberry.Mediator.Abstractions` and the DI helpers ship with `IsAotCompatible=true` on `net10.0` and `net11.0` (the `netstandard2.0` build is not annotated).
- The options-based `AddSnowberryMediator` / `AppendSnowberryMediator` carry `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` **even with explicit type lists**, because they inspect handler interfaces and close generic types with `MakeGenericType` at runtime. Calling them in a trimmed or AOT app produces `IL2026` / `IL3050` warnings.
- The source-generated `AddSnowberryMediator()` performs no assembly scanning and no `GetInterfaces` calls, and emits closed generic types as literals. It is the supported path for `PublishAot` and trimming; see the [AOT sample](samples/README.md).

## Performance

The `Send` and stream dispatch code (`Mediator.SendAsync`, the pipeline walker, the registries) contains no `async` methods: it returns the `ValueTask` / `ValueTask<T>` your handler chain produces without wrapping it in a state machine. `PublishAsync` runs through one library `async` method that completes synchronously, without allocating, when every handler completes synchronously. When a handler actually suspends at an `await`, its state machine is boxed (and, for `Publish`, the library's too).

Representative numbers on .NET 11 (`BenchmarkDotNet 0.16.0-preview.2`, `MemoryDiagnoser`, mean of 3 launches per benchmark, handlers registered as `Singleton`):

| Scenario                                  |    Latency | Allocated |
| ----------------------------------------- | ---------: | --------: |
| `Send_NoPipeline`                         |   ~12.8 ns |       0 B |
| `Send_Specific1`                          |   ~36.9 ns |       0 B |
| `Send_Specific3`                          |   ~66.8 ns |       0 B |
| `Send_Specific10`                         |  ~186.0 ns |       0 B |
| `Send_OpenGeneric1`                       |   ~43.6 ns |       0 B |
| `Send_OpenGeneric3`                       |   ~62.5 ns |       0 B |
| `Send_Mixed4` (2 specific + 2 generic)    |   ~75.2 ns |       0 B |
| `Publish_Specific3` (3 sync handlers)     |   ~44.1 ns |       0 B |
| `Publish_OpenGeneric3`                    |   ~47.2 ns |       0 B |
| `Publish_Mixed3`                          |   ~47.3 ns |       0 B |
| `Stream_NoPipeline_Enumerate10`           |  ~112.4 ns |     104 B¹ |
| `Stream_Specific1_Enumerate10`            |  ~261.0 ns |     352 B¹ |
| `Stream_Specific3_Enumerate10`            |  ~560.7 ns |     848 B¹ |
| `Stream_Specific10_Enumerate10`           | ~1559.0 ns |    2584 B¹ |
| `Stream_OpenGeneric3_Enumerate10`         |  ~646.6 ns |     848 B¹ |
| `Send_Specific1_Async` (awaiting handler) |  ~373.6 ns |     264 B² |

¹ The stream allocations are the compiler-generated `async IAsyncEnumerable<T>` enumerators in user code, one per behavior plus the handler.
² The 264 B is the user handler's `async ValueTask<T>` state-machine box. Its latency is dominated by the handler's `Task.Yield()` thread-pool hop. Opt into the pooled builder per method to recycle these (`net6.0` and later):

```csharp
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
public async ValueTask<MyResponse> HandleAsync(MyRequest request, CancellationToken cancellationToken = default)
{
    // ... uses await ...
}
```

How the library reaches these numbers:

- The pipeline and notification registries are resolved once in the `Mediator` constructor, not on every send.
- Behavior registrations are sorted when the registry is built (and rebuilt lazily after an append) and snapshotted into a `FrozenDictionary` on `net10.0` / `net11.0` (a `Dictionary` on `netstandard2.0`).
- Open-generic behavior types are closed once per `(TRequest, TResponse)` pair via `MakeGenericType` (or the generated resolver) and cached; notification open-generic handlers are cached per notification type.
- The request pipeline uses a `readonly struct` walker that implements `IPipelineContinuation<TRequest, TResponse>` and is passed by value through the chain, so chain steps are direct calls with no delegate allocation.
- A static-generic fast cache (`PipelineFastCache<TRequest, TResponse>`) holds the closed behavior types for a hot pair; a lookup is a single `Volatile.Read` plus an owner/generation check. When several registries alternate (for example in test suites), lookups fall back to a per-instance `ConcurrentDictionary`; that path stays correct but allocates a small cache entry per miss.
- The stream pipeline uses the same walker and `StreamPipelineFastCache` pattern; its remaining allocations are the `async IAsyncEnumerable<T>` enumerators in your code.

Benchmarks live in the `Snowberry.Mediator.Benchmarks` project. Run them with several launches, because dynamic PGO makes a single process land on noticeably different timings from run to run:

```bash
dotnet run -c Release -f net11.0 --project src/Snowberry.Mediator.Benchmarks -- --filter '*' --launchCount 3 --memory
```

## Pitfalls

- **Explicit type lists are not AOT-safe.** The options-based `AddSnowberryMediator` warns under trimming and Native AOT even without `Assemblies`. Use the source generator for `PublishAot`.
- **Scanning skips behaviors and notification handlers by default.** Setting `Assemblies` alone registers request and stream handlers only; set `ScanPipelineBehaviors`, `ScanStreamPipelineBehaviors` and `ScanNotificationHandlers` to pick up the rest.
- **A second `AddSnowberryMediator` can throw.** Once behaviors or notification handlers are registered, another non-append call that adds more throws `InvalidOperationException`. Use `append: true` or `AppendSnowberryMediator`.
- **Zero or negative priorities do not mean "last".** They are compared with registration positions and interleave with unattributed behaviors. Use positive priorities to move a behavior to the front, and list order for the rest.
- **`Publish` stops at the first failing handler.** Handlers run sequentially; later handlers do not run once one throws. Catch inside a handler if the others must still run.
- **Inject `IMediator`, not `IMediatorSender` / `IMediatorPublisher`.** Only `IMediator` is registered.
- **The 0 B numbers assume cheap resolution.** The benchmarks register handlers as `Singleton`; `Transient` handlers allocate on every resolve, and `Scoped` handlers need a scope.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator.Abstractions`](src/Snowberry.Mediator.Abstractions/README.md) | Message, handler and pipeline interfaces for libraries that only define handlers. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](src/Snowberry.Mediator.Extensions.DependencyInjection/README.md) | `Microsoft.Extensions.DependencyInjection` integration. |
| [`Snowberry.Mediator.DependencyInjection`](src/Snowberry.Mediator.DependencyInjection/README.md) | `Snowberry.DependencyInjection` integration. |
| [`Snowberry.Mediator.DependencyInjection.Shared`](src/Snowberry.Mediator.DependencyInjection.Shared/README.md) | Container-agnostic registration helper behind both integrations. |
| [`Snowberry.Mediator.SourceGenerator`](src/Snowberry.Mediator.SourceGenerator/README.md) | Compile-time, reflection-free registration for trimmed and Native AOT apps. |
| [`Snowberry.Mediator.OpenTelemetry.Shared`](src/Snowberry.Mediator.OpenTelemetry.Shared/README.md) | Container-agnostic OpenTelemetry decorator, conventions and builder extensions. |
| [`Snowberry.Mediator.Extensions.OpenTelemetry`](src/Snowberry.Mediator.Extensions.OpenTelemetry/README.md) | OpenTelemetry wiring for `Microsoft.Extensions.DependencyInjection`. |
| [`Snowberry.Mediator.OpenTelemetry`](src/Snowberry.Mediator.OpenTelemetry/README.md) | OpenTelemetry wiring for `Snowberry.DependencyInjection`. |

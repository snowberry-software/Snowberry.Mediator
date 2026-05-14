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
  - `IStreamPipelineBehavior<TRequest, TResponse>`
- Assembly scanning helper to discover handlers, pipeline behaviors and notification handlers (`MediatorAssemblyHelper`).
- Global registries for pipeline and notification handlers used at runtime by `Mediator`.
- Zero per-call allocation through the mediator's own dispatch code (see [Performance](#performance)). `SendAsync` / `PublishAsync` return `ValueTask` / `ValueTask<T>` and never allocate a state machine inside the library - any per-call allocation that remains comes from your own handlers if they use `async`/`await` and suspend.
- AOT-compatible: `Snowberry.Mediator`, `Snowberry.Mediator.Abstractions` and the DI helpers ship with `IsAotCompatible=true` on `net9.0+`. Assembly-scanning entry points are gated with `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`; the explicit-registration entry points are AOT-safe.

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

Stream pipeline behaviors (`IStreamPipelineBehavior<TRequest, TResponse>`) keep the classic `NextPipeline` delegate property - the compiler-generated `async IAsyncEnumerable<T>` state machine dominates allocation in that path, so the struct-continuation pattern would not produce a measurable improvement there.

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
| `Stream_NoPipeline_Enumerate10`           |  ~114.9 ns |     104 B¹ |
| `Stream_Specific3_Enumerate10`            |  ~560.2 ns |     936 B¹ |
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

Benchmarks live in the `Snowberry.Mediator.Benchmarks` project.
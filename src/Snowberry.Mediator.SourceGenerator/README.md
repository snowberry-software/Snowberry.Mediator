# Snowberry.Mediator.SourceGenerator

**A Roslyn incremental source generator for [`Snowberry.Mediator`](../../README.md).** It discovers request handlers, stream handlers, notification handlers and pipeline behaviors **at compile time**, across your project and its referenced assemblies, and emits the DI registration with literal closed generic types. The generated path does no assembly scanning, no `Type.GetInterfaces()` and no reachable `MakeGenericType`, so it is clean under trimming and Native AOT.

The package is a development dependency (it adds no runtime assembly of its own) and works with both DI integrations: [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) and [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md).

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Generated Entry Points](#generated-entry-points)
- [What Is Discovered](#what-is-discovered)
- [Scoping Which Assemblies Are Scanned](#scoping-which-assemblies-are-scanned)
- [Per-Handler Service Lifetime](#per-handler-service-lifetime)
- [Diagnostics](#diagnostics)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.SourceGenerator
dotnet add package Snowberry.Mediator.Extensions.DependencyInjection   # or Snowberry.Mediator.DependencyInjection
```

## Quick Start

1. Reference this package plus a DI integration package in your composition-root project.
2. Opt in with the assembly attribute (in `Program.cs` or an `AssemblyInfo.cs`).
3. Call the generated registration, with no `options.Assemblies` and no hand-listed handler types.

```csharp
using Microsoft.Extensions.DependencyInjection;

[assembly: Snowberry.Mediator.SnowberryMediator]

var services = new ServiceCollection();
services.AddSnowberryMediator();                           // default Scoped lifetime
// services.AddSnowberryMediator(ServiceLifetime.Singleton);
```

The attributes are generated `internal` into the `Snowberry.Mediator` namespace: write them fully qualified or add `using Snowberry.Mediator;`.

## Generated Entry Points

| Integration referenced | Generated method |
| --- | --- |
| `Snowberry.Mediator.Extensions.DependencyInjection` | `AddSnowberryMediator(this IServiceCollection services, ServiceLifetime lifetime = Scoped, bool append = false)` in namespace `Microsoft.Extensions.DependencyInjection` |
| `Snowberry.Mediator.DependencyInjection` | `AddSnowberryMediator(this IServiceRegistry registry, ServiceLifetime lifetime = Scoped, bool append = false)` in namespace `Snowberry.Mediator.DependencyInjection` |

- Both are emitted when both integrations are referenced.
- `append: true` adds the discovered handlers to an existing mediator setup. A second call without `append` throws `InvalidOperationException` when it would create a second pipeline or notification registry.
- Registration uses try-add semantics, so existing registrations are kept.

## What Is Discovered

Classes and structs implementing `IRequestHandler<,>`, `IStreamRequestHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>` or `IStreamPipelineBehavior<,>`:

- **Open-generic behaviors and notification handlers** are closed over every matching request or notification type from the scanned assemblies, honoring their generic constraints. Open-generic notification handlers run before concrete ones.
- **`[PipelineOverwritePriority]` ordering** is emitted into the registration and matches the reflection path.
- **Per-category toggles** on `[SnowberryMediator]`, all `true` by default: `RegisterRequestHandlers`, `RegisterStreamRequestHandlers`, `RegisterNotificationHandlers`, `RegisterPipelineBehaviors`, `RegisterStreamPipelineBehaviors`.
- **Skipped without a diagnostic:** open-generic request and stream handlers, and open generics whose type parameters do not map one-to-one onto the interface's.

Handlers in referenced assemblies are registered when the composition-root project can access them: public, or `internal` with `[InternalsVisibleTo]`.

## Scoping Which Assemblies Are Scanned

By default the generator scans the current assembly and every referenced assembly that directly references `Snowberry.Mediator.Abstractions`. To restrict discovery, set `ScanReferencedAssemblies = false` and name each referenced assembly to include with a marker type:

```csharp
using Snowberry.Mediator;

// Scan only this assembly plus the two named ones; ignore all other referenced handlers.
[assembly: SnowberryMediator(ScanReferencedAssemblies = false)]
[assembly: SnowberryMediatorAssembly(typeof(MyApp.Orders.PlaceOrderHandler))]
[assembly: SnowberryMediatorAssembly(typeof(MyApp.Billing.Marker))]
```

- The current (composition-root) assembly is always scanned.
- With `ScanReferencedAssemblies = true` (the default), `SnowberryMediatorAssembly` markers are ignored.
- Open-generic behaviors and handlers are closed only over request and notification types found in scanned assemblies, so excluding an assembly removes both its handlers and its message types.

Use this to stop an unwanted referenced assembly from registering handlers, for example when two assemblies handle the same request (`SBMED001`).

## Per-Handler Service Lifetime

`AddSnowberryMediator(lifetime)` applies one lifetime to every discovered type. Because the generated registrations use try-add semantics, a type you register first keeps your lifetime:

```csharp
// Request and stream handlers are registered under their handler interface.
services.AddSingleton<IRequestHandler<GetUser, string>, GetUserHandler>();

// Notification handlers and behaviors are registered as their concrete type.
services.AddSingleton<OrderAuditHandler>();

services.AddSnowberryMediator();   // skips the pre-registered ones
```

## Diagnostics

| ID | Severity | Meaning |
| --- | --- | --- |
| SBMED001 | Error | Duplicate request handler for the same request/response. |
| SBMED002 | Error | Duplicate stream request handler for the same request/response. |
| SBMED101 | Info | Handler is inaccessible to the consuming assembly (add `[InternalsVisibleTo]`) and was skipped. |
| SBMED102 | Warning | A handler's request, response or notification type is inaccessible; the handler was skipped. |
| SBMED103 | Info | A non-instantiable (abstract or static) type implements a handler interface and was skipped. |
| SBMED201 | Warning | Multiple `[SnowberryMediator]` attributes; the first one is used. |
| SBMED202 | Info | The generator ran but discovered no handlers; no registration is emitted. |

## Pitfalls

- **No attribute, no code.** Without `[assembly: SnowberryMediator]` nothing is generated, and with `SBMED202` no entry point is emitted, so the `AddSnowberryMediator()` call fails to compile.
- **Unqualified attributes need the namespace.** `[assembly: SnowberryMediator]` compiles only with `using Snowberry.Mediator;`.
- **Contracts-only assemblies must be scanned too.** With `ScanReferencedAssemblies = false`, messages that live in an assembly you did not list are not used to close open-generic behaviors and notification handlers.
- **"References Abstractions" means a direct reference.** An assembly that only gets `Snowberry.Mediator.Abstractions` transitively is not scanned by default.
- **Lifetime overrides depend on the service type.** Pre-register request and stream handlers by interface, notification handlers and behaviors by concrete type.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) | Target for the generated `IServiceCollection` entry point. |
| [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) | Target for the generated `IServiceRegistry` entry point. |
| [`Snowberry.Mediator.Abstractions`](../Snowberry.Mediator.Abstractions/README.md) | The interfaces the generator discovers. |

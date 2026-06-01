# Snowberry.Mediator.SourceGenerator

A Roslyn incremental source generator for [Snowberry.Mediator](https://github.com/snowberry-software/Snowberry.Mediator) that
discovers handlers, behaviors and notification handlers **at compile time** and emits the registration code with
literal closed generics, eliminating all runtime reflection (`Assembly.GetTypes()`, `Type.GetInterfaces()`,
`MakeGenericType`). The result is fully trim- and NativeAOT-friendly: no dynamic code is executed on the generated path.

## Easy setup

1. Reference this package **plus** `Snowberry.Mediator` and a DI integration package
   (`Snowberry.Mediator.Extensions.DependencyInjection` for `Microsoft.Extensions.DependencyInjection`,
   or `Snowberry.Mediator.DependencyInjection` for the Snowberry container).
2. Add the opt-in attribute to your composition-root project (e.g. in `Program.cs` or an `AssemblyInfo.cs`):

   ```csharp
   [assembly: SnowberryMediator]
   ```

3. Call the generated registration with no `options.Assemblies` and no hand-listed handler types:

   ```csharp
   services.AddSnowberryMediator();                       // default Scoped lifetime
   services.AddSnowberryMediator(ServiceLifetime.Singleton);
   ```

Handlers are discovered both in your project **and** in referenced assemblies, as long as the composition-root
project can access the type (public, or `internal` exposed via `[InternalsVisibleTo]`).

## What is discovered

Types implementing any of the Snowberry.Mediator marker interfaces (`IRequestHandler<,>`,
`IStreamRequestHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`, `IStreamPipelineBehavior<,>`),
including open-generic behaviors and open-generic notification handlers. Pipeline ordering via
`[PipelineOverwritePriority]` is honored, with byte-identical semantics to the reflection-based path.

The `[SnowberryMediator]` attribute exposes per-category toggles (all default `true`):
`RegisterRequestHandlers`, `RegisterStreamRequestHandlers`, `RegisterNotificationHandlers`,
`RegisterPipelineBehaviors`, `RegisterStreamPipelineBehaviors`.

## Scoping which assemblies are scanned

By default the generator scans the current assembly **and every referenced assembly that references
`Snowberry.Mediator.Abstractions`**. To restrict discovery, set `ScanReferencedAssemblies = false`
and name the referenced assemblies to include with one
`[assembly: SnowberryMediatorAssembly(typeof(AnyTypeInThatAssembly))]` per assembly:

```csharp
// Scan only this assembly plus the two named ones; ignore all other referenced handlers.
[assembly: SnowberryMediator(ScanReferencedAssemblies = false)]
[assembly: SnowberryMediatorAssembly(typeof(MyApp.Orders.PlaceOrderHandler))]
[assembly: SnowberryMediatorAssembly(typeof(MyApp.Billing.Marker))]
```

- The **current (composition-root) assembly is always scanned** because it carries the opt-in attribute.
- `ScanReferencedAssemblies = true` (the default) scans every eligible referenced assembly;
  `SnowberryMediatorAssembly` markers are ignored in that mode (everything is already scanned).
- Open-generic behaviors/handlers only close over request/notification types from scanned
  assemblies, so excluding an assembly removes both its handlers and its closure targets.

Use this to stop an unwanted referenced assembly's handlers from registering (surprise
registrations, or `SBMED001` duplicate-handler errors when two assemblies handle the same request).

## Per-handler service lifetime

`AddSnowberryMediator(lifetime)` applies one lifetime to every discovered handler. To give a
specific handler a different lifetime, **register it before** calling the generated entry point, because
the generated registrations use no-overwrite (`TryAdd`) semantics, so a pre-registered handler keeps
your lifetime:

```csharp
services.AddSingleton<IRequestHandler<GetUser, string>, GetUserHandler>(); // your chosen lifetime
services.AddSnowberryMediator();                                           // skips the pre-registered one
```

## Diagnostics

| ID | Severity | Meaning |
|----|----------|---------|
| SBMED001 | Error | Duplicate request handler for the same request/response. |
| SBMED002 | Error | Duplicate stream request handler for the same request/response. |
| SBMED101 | Info | Handler is inaccessible to the consuming assembly (add `[InternalsVisibleTo]`) and was skipped. |
| SBMED102 | Warning | A handler's request/response/notification type is inaccessible; the handler was skipped. |
| SBMED103 | Warning | A non-instantiable type implements a handler interface and was skipped. |
| SBMED201 | Warning | Multiple `[SnowberryMediator]` attributes; the first one is used. |
| SBMED202 | Info | The generator was triggered but discovered no handlers. |

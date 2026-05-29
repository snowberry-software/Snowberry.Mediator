# Snowberry.Mediator.SourceGenerator

A Roslyn incremental source generator for [Snowberry.Mediator](https://github.com/snowberry-software/Snowberry.Mediator) that
discovers handlers, behaviors and notification handlers **at compile time** and emits the registration code with
literal closed generics — eliminating all runtime reflection (`Assembly.GetTypes()`, `Type.GetInterfaces()`,
`MakeGenericType`). The result is fully trim- and NativeAOT-friendly: no dynamic code is executed on the generated path.

## Easy setup

1. Reference this package **plus** `Snowberry.Mediator` and a DI integration package
   (`Snowberry.Mediator.Extensions.DependencyInjection` for `Microsoft.Extensions.DependencyInjection`,
   or `Snowberry.Mediator.DependencyInjection` for the Snowberry container).
2. Add the opt-in attribute to your composition-root project (e.g. in `Program.cs` or an `AssemblyInfo.cs`):

   ```csharp
   [assembly: SnowberryMediator]
   ```

3. Call the generated registration — no `options.Assemblies`, no hand-listed handler types:

   ```csharp
   services.AddSnowberryMediator();                       // default Scoped lifetime
   services.AddSnowberryMediator(ServiceLifetime.Singleton);
   ```

Handlers are discovered both in your project **and** in referenced assemblies, as long as the composition-root
project can access the type (public, or `internal` exposed via `[InternalsVisibleTo]`).

## What is discovered

Types implementing any of the Snowberry.Mediator marker interfaces — `IRequestHandler<,>`,
`IStreamRequestHandler<,>`, `INotificationHandler<>`, `IPipelineBehavior<,>`, `IStreamPipelineBehavior<,>` —
including open-generic behaviors and open-generic notification handlers. Pipeline ordering via
`[PipelineOverwritePriority]` is honored, with byte-identical semantics to the reflection-based path.

The `[SnowberryMediator]` attribute exposes per-category toggles (all default `true`):
`RegisterRequestHandlers`, `RegisterStreamRequestHandlers`, `RegisterNotificationHandlers`,
`RegisterPipelineBehaviors`, `RegisterStreamPipelineBehaviors`.

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

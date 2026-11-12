# Snowberry.Mediator.DependencyInjection.Shared

**The container-agnostic registration helper behind the [`Snowberry.Mediator`](../../README.md) DI integrations.** `DependencyInjectionHelper` turns `MediatorOptions` into registrations of `IMediator`, handlers, behaviors and the global registries against any container that implements the small `IServiceContext` abstraction. It targets `net11.0`, `net10.0` and `netstandard2.0`.

Applications do not reference this package directly; pick the integration for your container:

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) |
| `Snowberry.DependencyInjection` | [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) |

Reference it when writing an integration for another container.

## Table of Contents

- [Installation](#installation)
- [Writing an Integration](#writing-an-integration)
- [API Reference](#api-reference)
- [Pitfalls](#pitfalls)
- [Related Packages](#related-packages)

## Installation

```bash
dotnet add package Snowberry.Mediator.DependencyInjection.Shared
```

## Writing an Integration

1. Implement `IServiceContext` over your container's registration API.
2. Expose an extension method that builds `MediatorOptions` and calls `DependencyInjectionHelper.AddSnowberryMediator`.

```csharp
using System.Diagnostics.CodeAnalysis;
using Snowberry.Mediator;
using Snowberry.Mediator.DependencyInjection.Shared;
using Snowberry.Mediator.DependencyInjection.Shared.Contracts;

public static class MyContainerMediatorExtensions
{
    [RequiresUnreferencedCode("Scans assemblies and inspects handler types.")]
    [RequiresDynamicCode("Closes generic handler types at runtime.")]
    public static MyContainer AddSnowberryMediator(this MyContainer container, Action<MediatorOptions> configure, bool append = false)
    {
        var options = new MediatorOptions();
        configure(options);

        IServiceContext context = new MyContainerServiceContext(container);
        DependencyInjectionHelper.AddSnowberryMediator(context, options, RegistrationServiceLifetime.Scoped, append);
        return container;
    }
}
```

The built-in `MicrosoftServiceContext` and `SnowberryServiceContext` (in the two integration packages) are complete reference implementations.

## API Reference

**`IServiceContext`** (namespace `Snowberry.Mediator.DependencyInjection.Shared.Contracts`):

| Member | Contract |
| --- | --- |
| `bool IsServiceRegistered<T>()` | Whether a registration for `T` exists. |
| `void TryRegister(Type serviceType, Type implementationType, RegistrationServiceLifetime lifetime)` | Add the registration only if `serviceType` is not registered yet. |
| `void TryRegister(Type serviceType, object instance)` | Add a singleton instance only if `serviceType` is not registered yet. |
| `T? TryToGetSingleton<T>(out bool found)` | Return the singleton instance registered for `T`; used by append to reuse the existing registries. |

**`DependencyInjectionHelper`** (namespace `Snowberry.Mediator.DependencyInjection.Shared`):

| Member | Purpose |
| --- | --- |
| `AddSnowberryMediator(context, options, lifetime, append, customCallback = null)` | Scans `options.Assemblies`, then registers everything. `[RequiresUnreferencedCode]`, `[RequiresDynamicCode]`. |
| `AddSnowberryMediatorNoScan(context, options, lifetime, append, customCallback)` | Registers only the explicit type lists; trim-safe but still `[RequiresDynamicCode]`. |
| `ScanAssembly(options, handlerCollection, assembly)` | Adds one assembly's handlers to a `HandlerCollection`. `[RequiresUnreferencedCode]`. |
| `CustomAddCallbackDelegate`, `HandlerCollection` | Hook for contributing extra handlers before registration, and the collection it fills. |

**`RegistrationServiceLifetime`** is the container-neutral lifetime enum the integrations map to their own. `MediatorOptions` itself lives in the core package (namespace `Snowberry.Mediator`).

## Pitfalls

- **`TryRegister` must add-if-absent.** The helper relies on try-add semantics: an existing `IMediator` or handler must survive, otherwise user overrides and the OpenTelemetry decorator are replaced.
- **`TryToGetSingleton` must return the registered instance.** Append adds handlers to the registry instances it gets back here; returning `found = false` for a registered registry makes append create a new registry that the container never uses, so the appended handlers are lost.
- **A second non-append call can throw.** When a registry for behaviors or notification handlers already exists, registering more without `append` throws `InvalidOperationException`.
- **No variant is AOT-safe.** Both helper entry points need dynamic code; the source generator is the AOT path.

## Related Packages

| Package | Purpose |
| --- | --- |
| [`Snowberry.Mediator`](../../README.md) | The mediator implementation and `MediatorOptions`. |
| [`Snowberry.Mediator.Extensions.DependencyInjection`](../Snowberry.Mediator.Extensions.DependencyInjection/README.md) | Reference integration for `Microsoft.Extensions.DependencyInjection`. |
| [`Snowberry.Mediator.DependencyInjection`](../Snowberry.Mediator.DependencyInjection/README.md) | Reference integration for `Snowberry.DependencyInjection`. |
| [`Snowberry.Mediator.SourceGenerator`](../Snowberry.Mediator.SourceGenerator/README.md) | Compile-time registration that also goes through `IServiceContext`. |

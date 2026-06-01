# Snowberry.Mediator.DependencyInjection.Shared

Container-agnostic helper library that registers `Snowberry.Mediator` handlers, pipeline behaviors, and notification handlers against any DI container that adapts to its `IServiceContext` abstraction.

Consumers normally do not reference this package directly. Pick the DI integration package matching the container in use:

| Container | Package |
| --- | --- |
| `Microsoft.Extensions.DependencyInjection` | `Snowberry.Mediator.Extensions.DependencyInjection` |
| `Snowberry.DependencyInjection` | `Snowberry.Mediator.DependencyInjection` |

## What's inside

- `DependencyInjectionHelper`: the registration entry point. Provides both an assembly-scanning variant (`AddSnowberryMediator`) and an AOT-friendly explicit-registration variant (`AddSnowberryMediatorNoScan`).
- `IServiceContext`: the abstraction each integration package implements to bridge to its container's registration API.
- `MediatorOptions`: exposes assembly scanning, explicit handler types, pipeline behavior types, and notification handler types.

Authors of additional DI integrations implement `IServiceContext` and call `DependencyInjectionHelper.AddSnowberryMediator` (or the no-scan variant) to register the mediator and its handlers.

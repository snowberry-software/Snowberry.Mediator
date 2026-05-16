# Snowberry.Mediator.Extensions.DependencyInjection

`Microsoft.Extensions.DependencyInjection` integration for `Snowberry.Mediator`.

Adds `AddSnowberryMediator` to `IServiceCollection`, registering the mediator, request and stream handlers, pipeline behaviors, and notification handlers.

For `Snowberry.DependencyInjection` consumers, use the `Snowberry.Mediator.DependencyInjection` package instead.

## Usage

### Explicit registration (AOT-friendly)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddSnowberryMediator(opt =>
{
    opt.RequestHandlerTypes        = [typeof(MyRequestHandler)];
    opt.PipelineBehaviorTypes      = [typeof(MyPipelineBehavior<,>)];
    opt.NotificationHandlerTypes   = [typeof(MyNotificationHandler)];
    opt.StreamRequestHandlerTypes  = [typeof(MyStreamRequestHandler)];
    opt.StreamPipelineBehaviorTypes = [typeof(MyStreamPipelineBehavior<,>)];
});
```

### Assembly scanning

```csharp
services.AddSnowberryMediator(opt =>
{
    opt.Assemblies = [typeof(MyRequestHandler).Assembly];
});
```

Assembly scanning uses reflection and is not AOT-compatible.

## Service lifetime

The optional `serviceLifetime` parameter (default `ServiceLifetime.Scoped`) controls the lifetime applied to the mediator and all registered handlers.

```csharp
services.AddSnowberryMediator(opt => /* ... */, ServiceLifetime.Singleton);
```

## OpenTelemetry instrumentation

Pair with `Snowberry.Mediator.Extensions.OpenTelemetry` to emit OpenTelemetry traces and metrics for every mediator dispatch.

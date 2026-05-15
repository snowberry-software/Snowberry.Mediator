using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class Snowberry_OpenTelemetryDecorationTests
{
    [Fact]
    public void CalledAfterAddSnowberryMediator_ThrowsInvalidOperationException()
    {
        using var container = new ServiceContainer();
        container.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Scoped);

        var ex = Assert.Throws<InvalidOperationException>(() => container.AddSnowberryMediatorOpenTelemetry());
        Assert.Contains("before AddSnowberryMediator", ex.Message);
    }

    [Fact]
    public void CalledTwice_IsIdempotent()
    {
        using var container = new ServiceContainer();
        container.AddSnowberryMediatorOpenTelemetry(lifetime: ServiceLifetime.Scoped);
        container.AddSnowberryMediatorOpenTelemetry(lifetime: ServiceLifetime.Scoped);
        container.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Scoped);

        var mediator = container.GetRequiredService<IMediator>();
        Assert.IsType<InstrumentedMediator>(mediator);
    }

    [Fact]
    public async Task Decorator_DelegatesToInnerMediator()
    {
        using var container = new ServiceContainer();
        container.AddSnowberryMediatorOpenTelemetry(lifetime: ServiceLifetime.Scoped);
        container.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Scoped);

        var mediator = container.GetRequiredService<IMediator>();
        var result = await mediator.SendAsync(new CounterRequest());
        Assert.Equal(5, result);
    }

    [Fact]
    public void ResolvedMediator_IsInstrumentedMediator()
    {
        using var container = new ServiceContainer();
        container.AddSnowberryMediatorOpenTelemetry(lifetime: ServiceLifetime.Scoped);
        container.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Scoped);

        var mediator = container.GetRequiredService<IMediator>();
        Assert.IsType<InstrumentedMediator>(mediator);
    }
}
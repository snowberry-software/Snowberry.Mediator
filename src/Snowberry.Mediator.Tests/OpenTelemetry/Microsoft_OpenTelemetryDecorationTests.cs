using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class Microsoft_OpenTelemetryDecorationTests
{
    [Fact]
    public void CalledBeforeAddSnowberryMediator_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSnowberryMediatorOpenTelemetry());
        Assert.Contains("AddSnowberryMediator", ex.Message);
    }

    [Fact]
    public void CalledTwice_IsIdempotent_AndDoesNotDoubleWrap()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry();
        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();
        var mediator = (InstrumentedMediator)sp.GetRequiredService<IMediator>();

        Assert.IsNotType<InstrumentedMediator>(mediator.Inner);
    }

    [Fact]
    public async Task Decorates_FactoryRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator>(sp => new Mediator(sp));
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Assert.IsType<InstrumentedMediator>(mediator);
        Assert.Equal(5, await mediator.SendAsync(new CounterRequest()));
    }

    [Fact]
    public async Task Decorates_InstanceRegistration_WithCustomFake()
    {
        var fake = new FakeMediator();
        var services = new ServiceCollection();
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Singleton);

        // Replace the IMediator registration with an instance-based one BEFORE OTel.
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType == typeof(IMediator))
            {
                services[i] = new ServiceDescriptor(typeof(IMediator), fake);
                break;
            }
        }

        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var decorator = Assert.IsType<InstrumentedMediator>(mediator);
        Assert.Same(fake, decorator.Inner);

        await mediator.SendAsync(new CounterRequest());
        Assert.Equal(1, fake.SendInvocations);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void Decorator_PreservesOriginalLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], lifetime);
        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();

        if (lifetime == ServiceLifetime.Singleton)
        {
            Assert.Same(sp.GetRequiredService<IMediator>(), sp.GetRequiredService<IMediator>());
        }
        else if (lifetime == ServiceLifetime.Scoped)
        {
            using var s1 = sp.CreateScope();
            using var s2 = sp.CreateScope();
            Assert.Same(s1.ServiceProvider.GetRequiredService<IMediator>(), s1.ServiceProvider.GetRequiredService<IMediator>());
            Assert.NotSame(s1.ServiceProvider.GetRequiredService<IMediator>(), s2.ServiceProvider.GetRequiredService<IMediator>());
        }
        else
        {
            using var scope = sp.CreateScope();
            Assert.NotSame(scope.ServiceProvider.GetRequiredService<IMediator>(), scope.ServiceProvider.GetRequiredService<IMediator>());
        }
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task Decorator_DispatchesCorrectly_UnderEveryLifetime(ServiceLifetime lifetime)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], lifetime);
        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();

        IMediator mediator;
        if (lifetime == ServiceLifetime.Singleton)
            mediator = sp.GetRequiredService<IMediator>();
        else
            mediator = sp.CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        Assert.IsType<InstrumentedMediator>(mediator);
        Assert.Equal(5, await mediator.SendAsync(new CounterRequest()));
    }

    [Fact]
    public void ResolvedMediator_IsInstrumentedMediator()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)], ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        Assert.IsType<InstrumentedMediator>(mediator);
        Assert.IsType<Mediator>(((InstrumentedMediator)mediator).Inner);
    }
}
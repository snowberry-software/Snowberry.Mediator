using Microsoft.Extensions.DependencyInjection;
using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions.Interfaces;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Exceptions;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;
using MicrosoftDI = Snowberry.Mediator.Extensions.DependencyInjection.ServiceCollectionExtensions;
using SnowberryDI = Snowberry.Mediator.DependencyInjection.ServiceCollectionExtensions;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Covers public-API hardening: not-found exception shape/messages and null-argument validation on the
/// reflection-based registration entrypoints.
/// </summary>
public class ExtensionAndExceptionTests : Common.MediatorTestBase
{
    [Fact]
    public async Task HandlerNotFound_ExposesRequestType()
    {
        // A mediator with no handler registered for CounterRequest.
        var services = new ServiceCollection();
        services.AddSnowberryMediator(o => o.NotificationHandlerTypes = []);
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            await mediator.SendAsync(new CounterRequest()));

        Assert.Equal(typeof(CounterRequest), ex.RequestType);
        Assert.False(ex.IsStream);
    }

    [Fact]
    public void PipelineBehaviorNotFound_MessageIsNegated()
    {
        var ex = new PipelineBehaviorNotFoundException(typeof(CounterRequest), isStream: false);
        Assert.StartsWith("No pipeline behavior found for request type", ex.Message);
        Assert.Equal(typeof(CounterRequest), ex.RequestType);
    }

    [Fact]
    public void Microsoft_AddSnowberryMediator_NullServices_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => MicrosoftDI.AddSnowberryMediator(services, _ => { }));
    }

    [Fact]
    public void Microsoft_AddSnowberryMediator_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddSnowberryMediator(configure: null!));
    }

    [Fact]
    public void Snowberry_AddSnowberryMediator_NullRegistry_Throws()
    {
        IServiceRegistry registry = null!;
        Assert.Throws<ArgumentNullException>(() => SnowberryDI.AddSnowberryMediator(registry, _ => { }));
    }

    [Fact]
    public void Snowberry_AddSnowberryMediator_NullConfigure_Throws()
    {
        using var container = new ServiceContainer();
        Assert.Throws<ArgumentNullException>(() => container.AddSnowberryMediator(configure: null!));
    }
}

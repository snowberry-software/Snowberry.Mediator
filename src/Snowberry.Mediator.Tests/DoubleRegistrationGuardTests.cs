using Microsoft.Extensions.DependencyInjection;
using Snowberry.DependencyInjection;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Requests;
using Snowberry.Mediator.Tests.OpenTelemetry;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// A second non-append registration must fail fast instead of silently orphaning the second call's behaviors /
/// notification handlers into a registry the container never resolves. The supported way to extend an existing
/// mediator is the append overload, which must keep working.
/// </summary>
public class DoubleRegistrationGuardTests : Common.MediatorTestBase
{
    [Fact]
    public void Microsoft_SecondNonAppendRegistration_WithBehaviors_Throws()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(o =>
        {
            o.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            o.PipelineBehaviorTypes = [typeof(FirstBehavior<,>)];
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddSnowberryMediator(o =>
            {
                o.RequestHandlerTypes = [typeof(CounterRequestHandler)];
                o.PipelineBehaviorTypes = [typeof(SecondBehavior<,>)];
            }));

        Assert.Contains("append", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Microsoft_SecondNonAppendRegistration_WithNotifications_Throws()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(o => o.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)]);

        Assert.Throws<InvalidOperationException>(() =>
            services.AddSnowberryMediator(o => o.NotificationHandlerTypes = [typeof(AnotherSimpleNotificationHandler)]));
    }

    [Fact]
    public void Snowberry_SecondNonAppendRegistration_WithBehaviors_Throws()
    {
        using var container = new ServiceContainer();
        container.AddSnowberryMediator(o =>
        {
            o.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            o.PipelineBehaviorTypes = [typeof(FirstBehavior<,>)];
        });

        Assert.Throws<InvalidOperationException>(() =>
            container.AddSnowberryMediator(o =>
            {
                o.RequestHandlerTypes = [typeof(CounterRequestHandler)];
                o.PipelineBehaviorTypes = [typeof(SecondBehavior<,>)];
            }));
    }

    [Fact]
    public void Microsoft_AppendAfterAdd_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(o =>
        {
            o.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            o.PipelineBehaviorTypes = [typeof(FirstBehavior<,>)];
        });

        // The supported extension path must remain unaffected by the guard.
        var ex = Record.Exception(() =>
            services.AddSnowberryMediator(o => o.PipelineBehaviorTypes = [typeof(SecondBehavior<,>)], append: true));

        Assert.Null(ex);
    }
}

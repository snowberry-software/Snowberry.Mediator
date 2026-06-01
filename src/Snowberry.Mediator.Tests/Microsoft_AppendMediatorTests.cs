using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Tests for the Microsoft <see cref="IServiceCollection"/> append support (the <c>append</c> parameter on
/// <see cref="ServiceCollectionExtensions.AddSnowberryMediator"/>), mirroring the Snowberry registry-side
/// <c>Snowberry_AppendMediatorTests</c>. Verifies a second registration extends the existing mediator instead
/// of being silently dropped.
/// </summary>
public class Microsoft_AppendMediatorTests : Common.MediatorTestBase
{
    [Fact]
    public async Task Test_Append_MergesNotificationHandlersIntoExistingRegistry()
    {
        NotificationHandlerExecutionTracker.Clear();

        var services = new ServiceCollection();

        // Host configures the mediator with only its own handler.
        services.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // A module extends the existing system by appending another handler for the same notification.
        services.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AnotherSimpleNotificationHandler)];
            options.RegisterNotificationHandlers = true;
        }, append: true);

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new SimpleNotification { Message = "hi" });

        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Contains(nameof(SimpleNotificationHandler), executions);
        Assert.Contains(nameof(AnotherSimpleNotificationHandler), executions);
    }
}

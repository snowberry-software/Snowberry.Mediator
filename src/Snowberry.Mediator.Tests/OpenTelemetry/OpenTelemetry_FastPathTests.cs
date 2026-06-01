using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;
using Snowberry.Mediator.Extensions.OpenTelemetry;
using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

/// <summary>
/// Verifies the fast path is functionally equivalent to the un-decorated mediator when no listener
/// / meter is attached. The "no allocation on fast path" assertion is owned by the benchmarks; this
/// test only validates correctness.
/// </summary>
[Collection("OpenTelemetry")]
public class OpenTelemetry_FastPathTests
{
    private static IMediator BuildMediatorWithOtel(Action<MediatorOptions> configureMediator)
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(configureMediator, ServiceLifetime.Singleton);
        services.AddSnowberryMediatorOpenTelemetry();
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task NoListener_CreateStreamAsync_EnumeratesAllItems()
    {
        var mediator = BuildMediatorWithOtel(opt =>
            opt.StreamRequestHandlerTypes = [typeof(NumberStreamRequestHandler)]);

        int count = 0;
        await foreach (var _ in mediator.CreateStreamAsync(new NumberStreamRequest { Count = 4 }))
            count++;
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task NoListener_PublishAsync_DispatchesAllHandlers()
    {
        var mediator = BuildMediatorWithOtel(opt =>
            opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)]);

        SimpleNotificationHandler.ClearReceivedNotifications();
        await mediator.PublishAsync(new SimpleNotification { Value = 42 });
        Assert.Contains(SimpleNotificationHandler.ReceivedNotifications, n => n.Value == 42);
    }

    [Fact]
    public async Task NoListener_SendAsync_PropagatesException()
    {
        var mediator = BuildMediatorWithOtel(opt => opt.RequestHandlerTypes = [typeof(ThrowingRequestHandler)]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await mediator.SendAsync(new ThrowingRequest()));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task NoListener_SendAsync_ReturnsSameResultAsUndecorated()
    {
        // No fixture → no listener subscribed.
        var mediator = BuildMediatorWithOtel(opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)]);
        var result = await mediator.SendAsync(new CounterRequest());
        Assert.Equal(5, result);
        Assert.Null(System.Diagnostics.Activity.Current);
    }
}
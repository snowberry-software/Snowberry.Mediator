using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_NotificationHandlerSpanTests
{
    [Fact]
    public async Task Disabled_ByDefault_NoHandlerActivities()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        });

        await fx.Mediator.PublishAsync(new SimpleNotification());

        Assert.Single(fx.StoppedActivities);
        Assert.StartsWith("Mediator.Publish", fx.StoppedActivities[0].OperationName);
    }

    [Fact]
    public async Task Enabled_ProducesHandlerActivities_ParentedToPublish()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)],
            tel => tel.EnableNotificationHandlerSpans = true);

        SimpleNotificationHandler.ClearReceivedNotifications();
        await fx.Mediator.PublishAsync(new SimpleNotification());

        Assert.Equal(2, fx.StoppedActivities.Count);
        var publish = Assert.Single(fx.StoppedActivities, a => a.OperationName.StartsWith("Mediator.Publish"));
        var handler = Assert.Single(fx.StoppedActivities, a => a.OperationName.StartsWith("Mediator.Handler"));
        Assert.Equal("SimpleNotificationHandler", handler.GetTagItem("snowberry.mediator.handler.type"));
        Assert.Equal("SimpleNotification", handler.GetTagItem("snowberry.mediator.notification.type"));
        Assert.Equal(publish.Id, handler.ParentId);
        Assert.Equal(publish.TraceId, handler.TraceId);
    }
}

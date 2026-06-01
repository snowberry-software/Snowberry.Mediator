using Snowberry.Mediator.OpenTelemetry;
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
        Assert.StartsWith(MediatorTelemetryConventions.ActivityNames.c_PublishPrefix, fx.StoppedActivities[0].OperationName);
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
        var publish = Assert.Single(fx.StoppedActivities, a => a.OperationName.StartsWith(MediatorTelemetryConventions.ActivityNames.c_PublishPrefix));
        var handler = Assert.Single(fx.StoppedActivities, a => a.OperationName.StartsWith(MediatorTelemetryConventions.ActivityNames.c_HandlerPrefix));
        Assert.Equal(typeof(SimpleNotificationHandler).FullName, handler.GetTagItem(MediatorTelemetryConventions.Tags.c_HandlerType));
        Assert.Equal(typeof(SimpleNotification).FullName, handler.GetTagItem(MediatorTelemetryConventions.Tags.c_NotificationType));
        Assert.Equal(publish.Id, handler.ParentId);
        Assert.Equal(publish.TraceId, handler.TraceId);
    }
}

using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_EnrichmentAndFilterTests
{
    [Fact]
    public async Task EnrichWithRequest_IsInvoked_WithActivityAndRequest()
    {
        object? capturedRequest = null;
        System.Diagnostics.Activity? capturedActivity = null;

        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.EnrichWithRequest = (a, r) => { capturedActivity = a; capturedRequest = r; a.SetTag("custom", "x"); });

        await fx.Mediator.SendAsync(new CounterRequest());

        Assert.NotNull(capturedActivity);
        Assert.IsType<CounterRequest>(capturedRequest);
        Assert.Equal("x", fx.StoppedActivities[0].GetTagItem("custom"));
    }

    [Fact]
    public async Task EnrichWithResponse_IsInvoked_OnSuccess()
    {
        object? capturedResponse = null;

        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.EnrichWithResponse = (_, _, response) => capturedResponse = response);

        await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Equal(5, capturedResponse);
    }

    [Fact]
    public async Task EnrichWithException_IsInvoked_OnFailure()
    {
        Exception? capturedException = null;

        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(ThrowingRequestHandler)],
            tel => tel.EnrichWithException = (_, _, ex) => capturedException = ex);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await fx.Mediator.SendAsync(new ThrowingRequest()));

        Assert.IsType<InvalidOperationException>(capturedException);
    }

    [Fact]
    public async Task EnrichWithNotification_IsInvoked()
    {
        object? captured = null;

        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)],
            tel => tel.EnrichWithNotification = (_, n) => captured = n);

        await fx.Mediator.PublishAsync(new SimpleNotification { Message = "hi" });

        Assert.IsType<SimpleNotification>(captured);
    }

    [Fact]
    public async Task EnrichmentHook_Throwing_DoesNotBreakDispatch()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.EnrichWithRequest = (_, _) => throw new Exception("hook failure"));

        var result = await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Equal(5, result);
        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Contains(activity.Events, e => e.Name == "snowberry.mediator.enrichment.failed");
    }

    [Fact]
    public async Task Filter_ReturnsFalse_SkipsActivityAndMetrics()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.Filter = _ => false);

        var result = await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Equal(5, result);
        Assert.Empty(fx.StoppedActivities);
        Assert.Empty(fx.Measurements);
    }

    [Fact]
    public async Task Filter_ReturnsTrue_AllowsActivityAndMetrics()
    {
        int filterInvocations = 0;
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.RequestHandlerTypes = [typeof(CounterRequestHandler)],
            tel => tel.Filter = _ => { filterInvocations++; return true; });

        await fx.Mediator.SendAsync(new CounterRequest());

        Assert.Equal(1, filterInvocations);
        Assert.NotEmpty(fx.StoppedActivities);
        Assert.NotEmpty(fx.Measurements);
    }
}

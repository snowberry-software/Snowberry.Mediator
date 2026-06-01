using System.Diagnostics;
using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

/// <summary>
/// Verifies stream dispatch records exception detail and invokes <see cref="MediatorTelemetryOptions.EnrichWithException"/>
/// when the stream throws, matching the Send/Publish paths, while NOT firing the hook when the consumer simply
/// stops enumerating early (an Error status with no exception).
/// </summary>
[Collection("OpenTelemetry")]
public class OpenTelemetry_StreamEnrichmentTests
{
    [Fact]
    public async Task StreamThrows_SetsStatusMessage_AndInvokesEnrichException()
    {
        Exception? enriched = null;
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.StreamRequestHandlerTypes = [typeof(ThrowingStreamRequestHandler)],
            tel => tel.EnrichWithException = (_, _, ex) => enriched = ex);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in fx.Mediator.CreateStreamAsync(new ThrowingStreamRequest()))
            {
            }
        });

        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("stream boom", activity.StatusDescription);
        Assert.NotNull(enriched);
        Assert.IsType<InvalidOperationException>(enriched);
    }

    [Fact]
    public async Task StreamEarlyBreak_SetsErrorStatus_ButDoesNotInvokeEnrichException()
    {
        bool hookFired = false;
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt => opt.StreamRequestHandlerTypes = [typeof(ThrowingStreamRequestHandler)],
            tel => tel.EnrichWithException = (_, _, _) => hookFired = true);

        // Consume only the first item, then stop enumerating before the handler throws.
        await foreach (var _ in fx.Mediator.CreateStreamAsync(new ThrowingStreamRequest()))
            break;

        var activity = Assert.Single(fx.StoppedActivities);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Null(activity.StatusDescription);
        Assert.False(hookFired);
    }
}

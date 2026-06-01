using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_ConcurrencyTests
{
    [Fact]
    public async Task SendAsync_HundredParallelDispatches_ProduceDistinctActivities()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
        });

        const int c_DispatchCount = 100;

        var tasks = new Task[c_DispatchCount];
        for (int i = 0; i < c_DispatchCount; i++)
            tasks[i] = fx.Mediator.SendAsync(new CounterRequest()).AsTask();

        await Task.WhenAll(tasks);

        Assert.Equal(c_DispatchCount, fx.StoppedActivities.Count);

        var ids = fx.StoppedActivities.Select(a => a.Id).ToHashSet();
        Assert.Equal(c_DispatchCount, ids.Count);
    }
}

using Snowberry.Mediator.Tests.Common.Handler;
using Snowberry.Mediator.Tests.Common.Requests;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_PipelineBehaviorSpanTests
{
    [Fact]
    public async Task Disabled_ByDefault_NoBehaviorActivities()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(opt =>
        {
            opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
            opt.PipelineBehaviorTypes = [typeof(FirstBehavior<,>), typeof(SecondBehavior<,>), typeof(ThirdBehavior<,>)];
        });

        await fx.Mediator.SendAsync(new CounterRequest());

        // Only the top-level dispatch activity.
        Assert.Single(fx.StoppedActivities);
        Assert.Equal("Mediator.Send CounterRequest", fx.StoppedActivities[0].OperationName);
    }

    [Fact]
    public async Task Enabled_ProducesBehaviorActivities_ParentedToDispatch()
    {
        using var fx = new MicrosoftOpenTelemetryFixture(
            opt =>
            {
                opt.RequestHandlerTypes = [typeof(CounterRequestHandler)];
                opt.PipelineBehaviorTypes = [typeof(FirstBehavior<,>), typeof(SecondBehavior<,>), typeof(ThirdBehavior<,>)];
            },
            tel => tel.EnablePipelineBehaviorSpans = true);

        await fx.Mediator.SendAsync(new CounterRequest());

        // 1 dispatch + 3 per-behavior spans = 4.
        Assert.Equal(4, fx.StoppedActivities.Count);

        var dispatch = Assert.Single(fx.StoppedActivities, a => a.OperationName.StartsWith("Mediator.Send"));
        var behaviors = fx.StoppedActivities.Where(a => a.OperationName.StartsWith("Mediator.Behavior")).ToList();
        Assert.Equal(3, behaviors.Count);

        foreach (var b in behaviors)
        {
            Assert.Equal(System.Diagnostics.ActivityKind.Internal, b.Kind);
            Assert.NotNull(b.GetTagItem("snowberry.mediator.behavior.type"));
            Assert.Equal("CounterRequest", b.GetTagItem("snowberry.mediator.request.type"));
            Assert.Equal(dispatch.TraceId, b.TraceId);
        }

        // The outermost behavior is parented directly to the dispatch activity.
        Assert.Contains(behaviors, b => b.ParentId == dispatch.Id);
    }
}

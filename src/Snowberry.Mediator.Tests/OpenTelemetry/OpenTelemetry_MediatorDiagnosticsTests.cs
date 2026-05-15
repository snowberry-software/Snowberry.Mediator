namespace Snowberry.Mediator.Tests.OpenTelemetry;

[Collection("OpenTelemetry")]
public class OpenTelemetry_MediatorDiagnosticsTests
{
    public OpenTelemetry_MediatorDiagnosticsTests() => MediatorDiagnostics.ResetForTests();

    [Fact]
    public void EnableNotificationSpans_FlipsFlag()
    {
        MediatorDiagnostics.EnableNotificationSpans();
        Assert.True(MediatorDiagnostics.IsNotificationEnabled);
        Assert.False(MediatorDiagnostics.IsPipelineEnabled);
    }

    [Fact]
    public void EnablePipelineSpans_FlipsFlag()
    {
        MediatorDiagnostics.EnablePipelineSpans();
        Assert.True(MediatorDiagnostics.IsPipelineEnabled);
        Assert.False(MediatorDiagnostics.IsNotificationEnabled);
    }

    [Fact]
    public void Enable_CalledTwice_IsIdempotent()
    {
        MediatorDiagnostics.EnablePipelineSpans();
        MediatorDiagnostics.EnablePipelineSpans();
        Assert.True(MediatorDiagnostics.IsPipelineEnabled);
    }

    [Fact]
    public void ResetForTests_LeavesBothFlagsFalse()
    {
        MediatorDiagnostics.ResetForTests();
        Assert.False(MediatorDiagnostics.IsPipelineEnabled);
        Assert.False(MediatorDiagnostics.IsNotificationEnabled);
    }

    [Fact]
    public void Sources_HaveCorrectNames()
    {
        Assert.Equal("Snowberry.Mediator.Pipeline", MediatorDiagnostics.PipelineSource.Name);
        Assert.Equal("Snowberry.Mediator.Notification", MediatorDiagnostics.NotificationSource.Name);
    }
}
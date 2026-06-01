namespace Snowberry.Mediator.OpenTelemetry.Internals;

/// <summary>
/// Shared bootstrap used by the container-specific OpenTelemetry registration entrypoints. Builds the
/// <see cref="MediatorTelemetryOptions"/> and applies the process-wide per-step span flags; the
/// container-specific decoration/registration logic stays in each entrypoint.
/// </summary>
internal static class TelemetryRegistration
{
    /// <summary>
    /// Creates a <see cref="MediatorTelemetryOptions"/>, invokes <paramref name="configure"/> if supplied, and
    /// enables the per-step pipeline/notification span flags according to the resulting options.
    /// </summary>
    /// <param name="configure">An optional callback used to configure the options.</param>
    /// <returns>The configured options.</returns>
    public static MediatorTelemetryOptions BuildOptions(Action<MediatorTelemetryOptions>? configure)
    {
        var options = new MediatorTelemetryOptions();
        configure?.Invoke(options);

        if (options.EnablePipelineBehaviorSpans)
            MediatorDiagnostics.EnablePipelineSpans();
        if (options.EnableNotificationHandlerSpans)
            MediatorDiagnostics.EnableNotificationSpans();

        return options;
    }
}

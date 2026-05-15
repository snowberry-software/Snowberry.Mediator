namespace Snowberry.Mediator.Tests.OpenTelemetry;

/// <summary>
/// All OpenTelemetry tests live in this collection so they execute serially within the test assembly.
/// <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Metrics.Meter"/>
/// have process-wide listener state — running these tests in parallel would cause one fixture's
/// listener to capture activities/measurements from another fixture, producing nondeterministic
/// failures.
/// </summary>
[CollectionDefinition("OpenTelemetry", DisableParallelization = true)]
public sealed class OpenTelemetryCollection { }

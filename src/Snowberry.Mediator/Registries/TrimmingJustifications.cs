namespace Snowberry.Mediator.Registries;

/// <summary>
/// Shared justification strings for the trimming/AOT suppressions applied to the registry dispatch members.
/// All registered handler/behavior types are explicitly registered (by the source generator or by explicit
/// option lists), never discovered through reflection on the suppressed members.
/// </summary>
internal static class TrimmingJustifications
{
    /// <summary>Justification for pipeline-behavior registry suppressions.</summary>
    public const string PipelineBehaviors = "Pipeline behaviors are explicitly registered, not discovered through reflection.";

    /// <summary>Justification for stream-pipeline-behavior registry suppressions.</summary>
    public const string StreamPipelineBehaviors = "Stream pipeline behaviors are explicitly registered, not discovered through reflection.";
}

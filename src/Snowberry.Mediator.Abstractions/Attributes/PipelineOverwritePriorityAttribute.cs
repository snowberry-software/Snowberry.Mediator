namespace Snowberry.Mediator.Abstractions.Attributes;

/// <summary>
/// Specifies the priority of a pipeline behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class PipelineOverwritePriorityAttribute : Attribute
{
    /// <summary>
    /// The priority of the pipeline behavior.
    /// </summary>
    /// <remarks>Higer values indicate higher priority, meaning they will be executed earlier in the pipeline.</remarks>
    public int Priority { get; set; }
}

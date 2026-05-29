using System.Reflection;
using Snowberry.Mediator.Abstractions.Attributes;

namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a pipeline behavior.
/// </summary>
public class PipelineBehaviorHandlerInfo : RequestHandlerInfo
{
    /// <summary>
    /// An optional, pre-resolved priority. When set (for example, baked by the source generator from
    /// <see cref="PipelineOverwritePriorityAttribute"/> at compile time), <see cref="TryGetPriority(out int)"/>
    /// returns this value instead of reading the attribute from <see cref="RequestHandlerInfo.HandlerType"/>.
    /// </summary>
    public int? PriorityOverride { get; init; }

    /// <summary>
    /// Gets the execution-order priority for this behavior, preferring <see cref="PriorityOverride"/> when set
    /// and otherwise reading <see cref="PipelineOverwritePriorityAttribute"/> from <see cref="RequestHandlerInfo.HandlerType"/>.
    /// </summary>
    /// <param name="priority">When this method returns <see langword="true"/>, receives the resolved priority.</param>
    /// <returns><see langword="true"/> if a priority was found; otherwise <see langword="false"/>.</returns>
    public bool TryGetPriority(out int priority)
    {
        if (PriorityOverride is int overridden)
        {
            priority = overridden;
            return true;
        }

        priority = 0;

        if (HandlerType.GetCustomAttribute<PipelineOverwritePriorityAttribute>() is PipelineOverwritePriorityAttribute pipelinePriorityAttribute)
        {
            priority = pipelinePriorityAttribute.Priority;
            return true;
        }

        return false;
    }
}
using System.Reflection;
using Snowberry.Mediator.Abstractions.Attributes;

namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a pipeline behavior.
/// </summary>
public class PipelineBehaviorHandlerInfo : RequestHandlerInfo
{
    /// <inheritdoc/>
    public bool TryGetPriority(out int priority)
    {
        priority = 0;

        if (HandlerType.GetCustomAttribute<PipelineOverwritePriorityAttribute>() is PipelineOverwritePriorityAttribute pipelinePriorityAttribute)
        {
            priority = pipelinePriorityAttribute.Priority;
            return true;
        }

        return false;
    }
}

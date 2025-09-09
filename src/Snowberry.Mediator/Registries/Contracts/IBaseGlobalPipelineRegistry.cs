using Snowberry.Mediator.Models;

namespace Snowberry.Mediator.Registries.Contracts;

/// <summary>
/// The base contract for a global pipeline registry.
/// </summary>
/// <typeparam name="T">The handler information type.</typeparam>
public interface IBaseGlobalPipelineRegistry<T> where T : PipelineBehaviorHandlerInfo
{
    /// <summary>
    /// Registers a pipeline behavior.
    /// </summary>
    /// <param name="pipelineBehaviorHandlerInfo">The pipeline behavior.</param>
    void Register(T pipelineBehaviorHandlerInfo);

    /// <summary>
    /// Gets whether the registry is empty.
    /// </summary>
    bool IsEmpty { get; }
}

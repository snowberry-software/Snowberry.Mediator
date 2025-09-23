using System.Reflection;
using Snowberry.Mediator.Abstractions.Attributes;

namespace Snowberry.Mediator;

/// <summary>
/// Mediator configuration options.
/// </summary>
public sealed class MediatorOptions
{
    /// <summary>
    /// Automatically register request handlers.
    /// </summary>
    public bool RegisterRequestHandlers { get; set; } = true;

    /// <summary>
    /// Automatically register stream request handlers.
    /// </summary>
    public bool RegisterStreamRequestHandlers { get; set; } = true;

    /// <summary>
    /// Automatically register notification handlers.
    /// </summary>
    public bool RegisterNotificationHandlers { get; set; } = true;

    /// <summary>
    /// Automatically register pipeline behaviors.
    /// </summary>
    public bool RegisterPipelineBehaviors { get; set; } = true;

    /// <summary>
    /// Automatically register stream pipeline behaviors.
    /// </summary>
    public bool RegisterStreamPipelineBehaviors { get; set; } = true;

    /// <summary>
    /// Used to automatically scan pipeline behaviors from assemblies.
    /// </summary>
    /// <remarks/The order can be defined using the <see cref="PipelineOverwritePriorityAttribute"/> attribute.</remarks>
    public bool ScanPipelineBehaviors { get; set; } = false;

    /// <summary>
    /// Used to automatically scan stream pipeline behaviors from assemblies.
    /// </summary>
    /// <remarks>The order can be defined using the <see cref="PipelineOverwritePriorityAttribute"/> attribute.</remarks>
    public bool ScanStreamPipelineBehaviors { get; set; } = false;

    /// <summary>
    /// Used to automatically scan notification handlers.
    /// </summary>
    public bool ScanNotificationHandlers { get; set; } = false;

    /// <summary>
    /// The request handler types to register.
    /// </summary>
    /// <remarks>Depends on <see cref="RegisterRequestHandlers"/>.</remarks>
    public List<Type>? RequestHandlerTypes { get; set; }

    /// <summary>
    /// The stream request handler types to register.
    /// </summary>
    /// <remarks>Depends on <see cref="RegisterStreamRequestHandlers"/>.</remarks>
    public List<Type>? StreamRequestHandlerTypes { get; set; }

    /// <summary>
    /// The pipeline behavior types to register.
    /// </summary>
    /// <remarks>Order is important. When combining with <see cref="RegisterPipelineBehaviors"/> the <see cref="PipelineOverwritePriorityAttribute"/> should be used.</remarks>
    public List<Type>? PipelineBehaviorTypes { get; set; }

    /// <summary>
    /// The stream pipeline behavior types to register.
    /// </summary>
    /// <remarks>Order is important. When combining with <see cref="RegisterStreamPipelineBehaviors"/> the <see cref="PipelineOverwritePriorityAttribute"/> should be used.</remarks>
    public List<Type>? StreamPipelineBehaviorTypes { get; set; }

    /// <summary>
    /// The notification handler types to register.
    /// </summary>
    public List<Type>? NotificationHandlerTypes { get; set; }

    /// <summary>
    /// The assemblies to scan.
    /// </summary>
    public List<Assembly>? Assemblies { get; set; }
}

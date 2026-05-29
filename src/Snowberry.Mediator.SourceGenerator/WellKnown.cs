namespace Snowberry.Mediator.SourceGenerator;

/// <summary>
/// Metadata names (for <c>Compilation.GetTypeByMetadataName</c>) and fully-qualified type names
/// (for emission) used by the generator.
/// </summary>
internal static class WellKnown
{
    public const string AbstractionsAssemblyName = "Snowberry.Mediator.Abstractions";

    // Marker interface metadata names.
    public const string IRequest = "Snowberry.Mediator.Abstractions.Messages.IRequest`2";
    public const string IStreamRequest = "Snowberry.Mediator.Abstractions.Messages.IStreamRequest`2";
    public const string INotification = "Snowberry.Mediator.Abstractions.Messages.INotification";
    public const string IRequestHandler = "Snowberry.Mediator.Abstractions.Handler.IRequestHandler`2";
    public const string IStreamRequestHandler = "Snowberry.Mediator.Abstractions.Handler.IStreamRequestHandler`2";
    public const string INotificationHandler = "Snowberry.Mediator.Abstractions.Handler.INotificationHandler`1";
    public const string IPipelineBehavior = "Snowberry.Mediator.Abstractions.Pipeline.IPipelineBehavior`2";
    public const string IStreamPipelineBehavior = "Snowberry.Mediator.Abstractions.Pipeline.IStreamPipelineBehavior`2";

    public const string PipelineOverwritePriorityAttribute =
        "Snowberry.Mediator.Abstractions.Attributes.PipelineOverwritePriorityAttribute";

    // Trigger attribute (emitted via post-initialization into the consumer compilation).
    public const string TriggerAttributeNamespace = "Snowberry.Mediator";
    public const string TriggerAttributeName = "SnowberryMediatorAttribute";
    public const string TriggerAttributeMetadataName = "Snowberry.Mediator.SnowberryMediatorAttribute";

    // DI container detection.
    public const string MicrosoftServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string SnowberryServiceRegistry = "Snowberry.DependencyInjection.Abstractions.Interfaces.IServiceRegistry";

    // Emitted, fully-qualified runtime type names (without the global:: prefix; the emitter adds it).
    public const string FqMediatorInterface = "global::Snowberry.Mediator.Abstractions.IMediator";
    public const string FqMediator = "global::Snowberry.Mediator.Mediator";
    public const string FqRequestHandlerInterface = "global::Snowberry.Mediator.Abstractions.Handler.IRequestHandler";
    public const string FqStreamRequestHandlerInterface = "global::Snowberry.Mediator.Abstractions.Handler.IStreamRequestHandler";

    public const string FqRequestHandlerInfo = "global::Snowberry.Mediator.Models.RequestHandlerInfo";
    public const string FqStreamRequestHandlerInfo = "global::Snowberry.Mediator.Models.StreamRequestHandlerInfo";
    public const string FqPipelineBehaviorHandlerInfo = "global::Snowberry.Mediator.Models.PipelineBehaviorHandlerInfo";
    public const string FqStreamPipelineBehaviorHandlerInfo = "global::Snowberry.Mediator.Models.StreamPipelineBehaviorHandlerInfo";
    public const string FqNotificationHandlerInfo = "global::Snowberry.Mediator.Models.NotificationHandlerInfo";

    public const string FqGlobalPipelineRegistry = "global::Snowberry.Mediator.Registries.GlobalPipelineRegistry";
    public const string FqGlobalStreamPipelineRegistry = "global::Snowberry.Mediator.Registries.GlobalStreamPipelineRegistry";
    public const string FqGlobalNotificationHandlerRegistry = "global::Snowberry.Mediator.Registries.GlobalNotificationHandlerRegistry";

    public const string FqIGlobalPipelineRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalPipelineRegistry";
    public const string FqIGlobalStreamPipelineRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalStreamPipelineRegistry";
    public const string FqIGlobalNotificationHandlerRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalNotificationHandlerRegistry";

    public const string FqIServiceContext = "global::Snowberry.Mediator.DependencyInjection.Shared.Contracts.IServiceContext";
    public const string FqRegistrationServiceLifetime = "global::Snowberry.Mediator.DependencyInjection.Shared.RegistrationServiceLifetime";

    public const string FqMicrosoftServiceContext = "global::Snowberry.Mediator.Extensions.DependencyInjection.MicrosoftServiceContext";
    public const string FqMicrosoftServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string FqMicrosoftServiceLifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime";

    public const string FqSnowberryServiceContext = "global::Snowberry.Mediator.DependencyInjection.SnowberryServiceContext";
    public const string FqSnowberryServiceRegistry = "global::Snowberry.DependencyInjection.Abstractions.Interfaces.IServiceRegistry";
    public const string FqSnowberryServiceLifetime = "global::Snowberry.DependencyInjection.Abstractions.ServiceLifetime";

    public const string GeneratedNamespace = "Snowberry.Mediator.Generated";
    public const string FqGeneratedRegistration = "global::Snowberry.Mediator.Generated.SnowberryMediatorRegistration";
}

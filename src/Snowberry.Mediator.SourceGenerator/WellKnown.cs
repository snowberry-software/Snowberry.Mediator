namespace Snowberry.Mediator.SourceGenerator;

/// <summary>
/// Metadata names (for <c>Compilation.GetTypeByMetadataName</c>) and fully-qualified type names
/// (for emission) used by the generator.
/// </summary>
internal static class WellKnown
{
    public const string c_AbstractionsAssemblyName = "Snowberry.Mediator.Abstractions";

    // Marker interface metadata names.
    public const string c_IRequest = "Snowberry.Mediator.Abstractions.Messages.IRequest`2";
    public const string c_IStreamRequest = "Snowberry.Mediator.Abstractions.Messages.IStreamRequest`2";
    public const string c_INotification = "Snowberry.Mediator.Abstractions.Messages.INotification";
    public const string c_IRequestHandler = "Snowberry.Mediator.Abstractions.Handler.IRequestHandler`2";
    public const string c_IStreamRequestHandler = "Snowberry.Mediator.Abstractions.Handler.IStreamRequestHandler`2";
    public const string c_INotificationHandler = "Snowberry.Mediator.Abstractions.Handler.INotificationHandler`1";
    public const string c_IPipelineBehavior = "Snowberry.Mediator.Abstractions.Pipeline.IPipelineBehavior`2";
    public const string c_IStreamPipelineBehavior = "Snowberry.Mediator.Abstractions.Pipeline.IStreamPipelineBehavior`2";

    public const string c_PipelineOverwritePriorityAttribute =
        "Snowberry.Mediator.Abstractions.Attributes.PipelineOverwritePriorityAttribute";

    // Trigger attribute (emitted via post-initialization into the consumer compilation).
    public const string c_TriggerAttributeNamespace = "Snowberry.Mediator";
    public const string c_TriggerAttributeName = "SnowberryMediatorAttribute";
    public const string c_TriggerAttributeMetadataName = "Snowberry.Mediator.SnowberryMediatorAttribute";

    // Per-assembly include attribute (emitted via post-initialization), used when ScanReferencedAssemblies is false.
    public const string c_AssemblyAttributeMetadataName = "Snowberry.Mediator.SnowberryMediatorAssemblyAttribute";

    // DI integration detection. The bridge context type is probed rather than the container abstraction: its
    // presence proves the matching Snowberry integration package is referenced, which transitively guarantees
    // the container abstraction and lifetime enum that the generated entry point also depends on.
    public const string c_MicrosoftServiceContext = "Snowberry.Mediator.Extensions.DependencyInjection.MicrosoftServiceContext";
    public const string c_SnowberryServiceContext = "Snowberry.Mediator.DependencyInjection.SnowberryServiceContext";

    // Emitted, fully-qualified runtime type names (without the global:: prefix; the emitter adds it).
    public const string c_FqMediatorInterface = "global::Snowberry.Mediator.Abstractions.IMediator";
    public const string c_FqMediator = "global::Snowberry.Mediator.Mediator";
    public const string c_FqRequestHandlerInterface = "global::Snowberry.Mediator.Abstractions.Handler.IRequestHandler";
    public const string c_FqStreamRequestHandlerInterface = "global::Snowberry.Mediator.Abstractions.Handler.IStreamRequestHandler";

    public const string c_FqRequestHandlerInfo = "global::Snowberry.Mediator.Models.RequestHandlerInfo";
    public const string c_FqStreamRequestHandlerInfo = "global::Snowberry.Mediator.Models.StreamRequestHandlerInfo";
    public const string c_FqPipelineBehaviorHandlerInfo = "global::Snowberry.Mediator.Models.PipelineBehaviorHandlerInfo";
    public const string c_FqStreamPipelineBehaviorHandlerInfo = "global::Snowberry.Mediator.Models.StreamPipelineBehaviorHandlerInfo";
    public const string c_FqNotificationHandlerInfo = "global::Snowberry.Mediator.Models.NotificationHandlerInfo";

    public const string c_FqGlobalPipelineRegistry = "global::Snowberry.Mediator.Registries.GlobalPipelineRegistry";
    public const string c_FqGlobalStreamPipelineRegistry = "global::Snowberry.Mediator.Registries.GlobalStreamPipelineRegistry";
    public const string c_FqGlobalNotificationHandlerRegistry = "global::Snowberry.Mediator.Registries.GlobalNotificationHandlerRegistry";

    public const string c_FqIGlobalPipelineRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalPipelineRegistry";
    public const string c_FqIGlobalStreamPipelineRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalStreamPipelineRegistry";
    public const string c_FqIGlobalNotificationHandlerRegistry = "global::Snowberry.Mediator.Registries.Contracts.IGlobalNotificationHandlerRegistry";

    public const string c_FqIServiceContext = "global::Snowberry.Mediator.DependencyInjection.Shared.Contracts.IServiceContext";
    public const string c_FqRegistrationServiceLifetime = "global::Snowberry.Mediator.DependencyInjection.Shared.RegistrationServiceLifetime";

    public const string c_FqMicrosoftServiceContext = "global::Snowberry.Mediator.Extensions.DependencyInjection.MicrosoftServiceContext";
    public const string c_FqMicrosoftServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
    public const string c_FqMicrosoftServiceLifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime";

    public const string c_FqSnowberryServiceContext = "global::Snowberry.Mediator.DependencyInjection.SnowberryServiceContext";
    public const string c_FqSnowberryServiceRegistry = "global::Snowberry.DependencyInjection.Abstractions.Interfaces.IServiceRegistry";
    public const string c_FqSnowberryServiceLifetime = "global::Snowberry.DependencyInjection.Abstractions.ServiceLifetime";

    // Emitted member names (simple identifiers written into the generated source). The generator cannot
    // reference the runtime types directly (RS1038 / self-contained analyzer), so these are pinned to the real
    // members by WellKnownNamesTests: renaming a referenced property or method breaks that test's build.
    public const string c_PropHandlerType = "HandlerType";
    public const string c_PropRequestType = "RequestType";
    public const string c_PropResponseType = "ResponseType";
    public const string c_PropNotificationType = "NotificationType";
    public const string c_PropPriorityOverride = "PriorityOverride";

    public const string c_MethodRegister = "Register";
    public const string c_MethodBuild = "Build";
    public const string c_MethodIsServiceRegistered = "IsServiceRegistered";
    public const string c_MethodTryRegister = "TryRegister";
    public const string c_MethodTryToGetSingleton = "TryToGetSingleton";
    public const string c_MethodCreate = "Create";
    public const string c_MethodMakeGenericType = "MakeGenericType";

    public const string c_GeneratedNamespace = "Snowberry.Mediator.Generated";
    public const string c_FqGeneratedRegistration = "global::Snowberry.Mediator.Generated.SnowberryMediatorRegistration";
}

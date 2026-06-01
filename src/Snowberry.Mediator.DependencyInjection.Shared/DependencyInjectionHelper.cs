using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.DependencyInjection.Shared.Contracts;
using Snowberry.Mediator.Extensions;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.DependencyInjection.Shared;

/// <summary>
/// Container-agnostic helper that registers Mediator services into any <see cref="IServiceContext"/>
/// implementation. Provides both a fully-AOT-friendly explicit-registration variant
/// (<see cref="AddSnowberryMediatorNoScan"/>) and a reflection-based assembly-scanning variant
/// (<see cref="AddSnowberryMediator"/>).
/// </summary>
public static class DependencyInjectionHelper
{
    /// <summary>
    /// Callback signature invoked by <see cref="AddSnowberryMediatorNoScan"/> and
    /// <see cref="AddSnowberryMediator"/> after the <see cref="HandlerCollection"/> is created but before
    /// handler registrations are applied. Allows callers to inject additional handler types into the
    /// collection.
    /// </summary>
    /// <param name="serviceContext">The service context being populated.</param>
    /// <param name="options">The mediator options.</param>
    /// <param name="serviceLifetime">The lifetime applied to registered services.</param>
    /// <param name="handlerCollection">The mutable handler collection populated during registration.</param>
    /// <param name="append">Whether registrations are appended to existing services
    /// (<see langword="true"/>) or replace them (<see langword="false"/>).</param>
    public delegate void CustomAddCallbackDelegate(
        IServiceContext serviceContext,
        MediatorOptions options,
        RegistrationServiceLifetime serviceLifetime,
        HandlerCollection handlerCollection,
        bool append);

    /// <summary>
    /// Adds Mediator services to the specified service context.
    /// </summary>
    /// <param name="serviceContext">The service context.</param>
    /// <param name="options">The options.</param>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="append">Whether to append to existing registrations or replace them.</param>
    /// <param name="customCallback">A custom callback to execute at the start during registration.</param>
    /// <exception cref="InvalidOperationException">A mediator registry is already present on <paramref name="serviceContext"/> and <paramref name="append"/> is <see langword="false"/>.</exception>
    [RequiresUnreferencedCode("Assembly scanning requires unreferenced code. Use explicit handler registration for AOT compatibility.")]
    [RequiresDynamicCode("Creating generic handler types at runtime requires dynamic code. Use explicit handler registration for AOT compatibility.")]
    public static void AddSnowberryMediator(
        IServiceContext serviceContext,
        MediatorOptions options,
        RegistrationServiceLifetime serviceLifetime,
        bool append,
        CustomAddCallbackDelegate? customCallback = null)
    {
        AddSnowberryMediatorNoScan(serviceContext, options, serviceLifetime, append, (_, _, _, handlerCollection, _) =>
        {
            if (options.Assemblies != null && options.Assemblies.Count > 0)
            {
                for (int i = 0; i < options.Assemblies.Count; i++)
                {
                    var assembly = options.Assemblies[i];
                    ScanAssembly(options, handlerCollection, assembly);
                }
            }

            customCallback?.Invoke(serviceContext, options, serviceLifetime, handlerCollection, append);
        });
    }

    /// <summary>
    /// Adds Mediator services to the specified service context.
    /// </summary>
    /// <remarks>This variant ignores the <see cref="MediatorOptions.Assemblies"/> option to be more compatible with AOT scenarios.</remarks>
    /// <param name="serviceContext">The service context.</param>
    /// <param name="options">The options.</param>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="append">Whether to append to existing registrations or replace them.</param>
    /// <param name="customCallback">A custom callback to execute at the start during registration.</param>
    /// <exception cref="InvalidOperationException">A mediator registry is already present on <paramref name="serviceContext"/> and <paramref name="append"/> is <see langword="false"/>.</exception>
    [RequiresDynamicCode("Creating generic handler types at runtime requires dynamic code. Use explicit handler registration for AOT compatibility.")]
    public static void AddSnowberryMediatorNoScan(
        IServiceContext serviceContext,
        MediatorOptions options,
        RegistrationServiceLifetime serviceLifetime,
        bool append,
        CustomAddCallbackDelegate? customCallback)
    {
        if (!append || !serviceContext.IsServiceRegistered<IMediator>())
            serviceContext.TryRegister(typeof(IMediator), typeof(Mediator), serviceLifetime);

        var handlerCollection = new HandlerCollection();

        customCallback?.Invoke(serviceContext, options, serviceLifetime, handlerCollection, append);

        var pipelineBehaviorType = typeof(IPipelineBehavior<,>);
        var streamPipelineBehaviorType = typeof(IStreamPipelineBehavior<,>);
        var requestHandlerType = typeof(IRequestHandler<,>);
        var streamRequestHandlerType = typeof(IStreamRequestHandler<,>);

        if (options.PipelineBehaviorTypes != null && options.RegisterPipelineBehaviors)
            MediatorAssemblyHelper.ParseHandlerInfo(pipelineBehaviorType, options.PipelineBehaviorTypes, handlerCollection.AllPipelineBehaviorHandlers);

        if (options.RequestHandlerTypes != null && options.RegisterRequestHandlers)
            MediatorAssemblyHelper.ParseHandlerInfo(requestHandlerType, options.RequestHandlerTypes, handlerCollection.AllHandlers);

        if (options.StreamRequestHandlerTypes != null && options.RegisterStreamRequestHandlers)
            MediatorAssemblyHelper.ParseHandlerInfo(streamRequestHandlerType, options.StreamRequestHandlerTypes, handlerCollection.AllStreamHandlers);

        if (options.StreamPipelineBehaviorTypes != null && options.RegisterStreamPipelineBehaviors)
            MediatorAssemblyHelper.ParseHandlerInfo(streamPipelineBehaviorType, options.StreamPipelineBehaviorTypes, handlerCollection.AllStreamPipelineBehaviorHandlers);

        if (options.NotificationHandlerTypes != null && options.RegisterNotificationHandlers)
            MediatorAssemblyHelper.ParseNotificationHandlers(options.NotificationHandlerTypes, handlerCollection.AllNotificationHandlers);

        for (int i = 0; i < handlerCollection.AllHandlers.Count; i++)
        {
            var handlerInfo = handlerCollection.AllHandlers[i];
            serviceContext.TryRegister(handlerInfo.CreateRequestHandlerInterfaceType(), handlerInfo.HandlerType, serviceLifetime);
        }

        for (int i = 0; i < handlerCollection.AllStreamHandlers.Count; i++)
        {
            var handlerInfo = handlerCollection.AllStreamHandlers[i];
            serviceContext.TryRegister(handlerInfo.CreateStreamRequestHandlerInterfaceType(), handlerInfo.HandlerType, serviceLifetime);
        }

        if (options.RegisterPipelineBehaviors && handlerCollection.AllPipelineBehaviorHandlers.Count > 0)
            AddPipelineBehaviors<IGlobalPipelineRegistry, GlobalPipelineRegistry, PipelineBehaviorHandlerInfo>(
                serviceContext,
                serviceLifetime,
                handlerCollection.AllPipelineBehaviorHandlers,
                append);

        if (options.RegisterStreamPipelineBehaviors && handlerCollection.AllStreamPipelineBehaviorHandlers.Count > 0)
            AddPipelineBehaviors<IGlobalStreamPipelineRegistry, GlobalStreamPipelineRegistry, StreamPipelineBehaviorHandlerInfo>(
                serviceContext,
                serviceLifetime,
                handlerCollection.AllStreamPipelineBehaviorHandlers,
                append);

        if (options.RegisterNotificationHandlers && handlerCollection.AllNotificationHandlers.Count > 0)
            AddNotificationHandlers(serviceContext, serviceLifetime, handlerCollection.AllNotificationHandlers, append);
    }

    /// <summary>
    /// Scans an assembly for mediator handler implementations and adds the discovered types to
    /// <paramref name="handlerCollection"/>. Honours the <c>Register*</c> and <c>Scan*</c> flags on
    /// <paramref name="options"/> to determine which handler categories to include.
    /// </summary>
    /// <remarks>
    /// Request and stream-request handlers are gated on <see cref="MediatorOptions.RegisterRequestHandlers"/> /
    /// <see cref="MediatorOptions.RegisterStreamRequestHandlers"/> alone (no <c>Scan*</c> flag), while pipeline
    /// behaviors, stream pipeline behaviors, and notification handlers additionally require their
    /// <see cref="MediatorOptions.ScanPipelineBehaviors"/> / <see cref="MediatorOptions.ScanStreamPipelineBehaviors"/> /
    /// <see cref="MediatorOptions.ScanNotificationHandlers"/> flag.
    /// </remarks>
    /// <param name="options">The mediator options controlling which handler categories to scan.</param>
    /// <param name="handlerCollection">The destination collection that receives discovered handlers.</param>
    /// <param name="assembly">The assembly to scan.</param>
    [RequiresUnreferencedCode("Assembly scanning requires unreferenced code. Use explicit handler registration for AOT compatibility.")]
    public static void ScanAssembly(MediatorOptions options, HandlerCollection handlerCollection, Assembly assembly)
    {
        var scanResult = MediatorAssemblyHelper.ScanAssembly(assembly);

        if (options.RegisterRequestHandlers && scanResult.RequestHandlerTypes != null)
            for (int j = 0; j < scanResult.RequestHandlerTypes.Count; j++)
                handlerCollection.AllHandlers.Add(scanResult.RequestHandlerTypes[j]);

        if (options.RegisterStreamRequestHandlers && scanResult.StreamRequestHandlerTypes != null)
            for (int j = 0; j < scanResult.StreamRequestHandlerTypes.Count; j++)
                handlerCollection.AllStreamHandlers.Add(scanResult.StreamRequestHandlerTypes[j]);

        if (options.RegisterPipelineBehaviors && options.ScanPipelineBehaviors && scanResult.PipelineBehaviorTypes != null)
            for (int j = 0; j < scanResult.PipelineBehaviorTypes.Count; j++)
                handlerCollection.AllPipelineBehaviorHandlers.Add(scanResult.PipelineBehaviorTypes[j]);

        if (options.RegisterStreamPipelineBehaviors && options.ScanStreamPipelineBehaviors && scanResult.StreamPipelineBehaviorTypes != null)
            for (int j = 0; j < scanResult.StreamPipelineBehaviorTypes.Count; j++)
                handlerCollection.AllStreamPipelineBehaviorHandlers.Add(scanResult.StreamPipelineBehaviorTypes[j]);

        if (options.RegisterNotificationHandlers && options.ScanNotificationHandlers && scanResult.NotificationHandlerTypes != null)
            for (int j = 0; j < scanResult.NotificationHandlerTypes.Count; j++)
                handlerCollection.AllNotificationHandlers.Add(scanResult.NotificationHandlerTypes[j]);
    }

    private static void AddNotificationHandlers(
        IServiceContext serviceContext,
        RegistrationServiceLifetime serviceLifetime,
        IList<NotificationHandlerInfo> notificationHandlers,
        bool append
    )
    {
        if (notificationHandlers.Count == 0)
            return;

        bool alreadyRegistered = serviceContext.IsServiceRegistered<IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>>();

        // A non-append call that finds an existing registry would orphan its handlers into a fresh instance the
        // container never resolves (TryRegister is a no-op when the type already exists). Fail fast instead.
        if (alreadyRegistered && !append)
            throw new InvalidOperationException(
                "Snowberry.Mediator is already registered on this container. Call AppendSnowberryMediator (or pass append: true) to add more notification handlers.");

        IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>? globalNotificationHandlerRegistry;

        if (!alreadyRegistered)
        {
            globalNotificationHandlerRegistry = new GlobalNotificationHandlerRegistry();
            serviceContext.TryRegister(serviceType: typeof(IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>), instance: globalNotificationHandlerRegistry);
        }
        else
        {
            globalNotificationHandlerRegistry = serviceContext.TryToGetSingleton<IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>>(out bool foundSingleton);

            if (!foundSingleton)
            {
                globalNotificationHandlerRegistry = new GlobalNotificationHandlerRegistry();
                serviceContext.TryRegister(serviceType: typeof(IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>), instance: globalNotificationHandlerRegistry);
            }
        }

        for (int i = 0; i < notificationHandlers.Count; i++)
        {
            var handler = notificationHandlers[i];
            globalNotificationHandlerRegistry!.Register(handler);

            serviceContext.TryRegister(handler.HandlerType, handler.HandlerType, serviceLifetime);
        }

        // Snapshot the registered handlers into the read-optimized frozen state.
        globalNotificationHandlerRegistry!.Build();
    }

    private static void AddPipelineBehaviors<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TGlobalPipelineInterface, TGlobalPipelineRegistry, THandlerInfo>(
        IServiceContext serviceContext,
        RegistrationServiceLifetime serviceLifetime,
        IList<THandlerInfo> pipelineBehaviorHandlers,
        bool append
    )
        where TGlobalPipelineRegistry : TGlobalPipelineInterface, new()
        where TGlobalPipelineInterface : IBaseGlobalPipelineRegistry<THandlerInfo>
        where THandlerInfo : PipelineBehaviorHandlerInfo
    {
        if (pipelineBehaviorHandlers.Count == 0)
            return;

        bool alreadyRegistered = serviceContext.IsServiceRegistered<TGlobalPipelineInterface>();

        // A non-append call that finds an existing registry would orphan its behaviors into a fresh instance the
        // container never resolves (TryRegister is a no-op when the type already exists). Fail fast instead.
        if (alreadyRegistered && !append)
            throw new InvalidOperationException(
                "Snowberry.Mediator is already registered on this container. Call AppendSnowberryMediator (or pass append: true) to add more pipeline behaviors.");

        TGlobalPipelineInterface? globalPipelineRegistry;
        if (!alreadyRegistered)
        {
            globalPipelineRegistry = new TGlobalPipelineRegistry();
            serviceContext.TryRegister(serviceType: typeof(TGlobalPipelineInterface), instance: globalPipelineRegistry);
        }
        else
        {
            globalPipelineRegistry = serviceContext.TryToGetSingleton<TGlobalPipelineInterface>(out bool foundSingleton);

            if (!foundSingleton)
            {
                globalPipelineRegistry = new TGlobalPipelineRegistry();
                serviceContext.TryRegister(serviceType: typeof(TGlobalPipelineInterface), instance: globalPipelineRegistry);
            }
        }

        for (int i = 0; i < pipelineBehaviorHandlers.Count; i++)
        {
            var handler = pipelineBehaviorHandlers[i];
            globalPipelineRegistry!.Register(handler);

            serviceContext.TryRegister(handler.HandlerType, handler.HandlerType, serviceLifetime);
        }

        // Snapshot the registered behaviors into the read-optimized frozen state.
        globalPipelineRegistry!.Build();
    }

    /// <summary>
    /// Mutable collection of handler-info entries gathered during registration. Each list holds the
    /// handlers of one category (request, stream request, pipeline behavior, stream pipeline behavior,
    /// notification handler) before they are registered with the service context.
    /// </summary>
    public class HandlerCollection
    {
        /// <summary>Registered <see cref="IRequestHandler{TRequest, TResponse}"/> handler-info entries.</summary>
        public readonly List<RequestHandlerInfo> AllHandlers = [];

        /// <summary>Registered <see cref="INotificationHandler{TNotification}"/> handler-info entries.</summary>
        public readonly List<NotificationHandlerInfo> AllNotificationHandlers = [];

        /// <summary>Registered <see cref="IPipelineBehavior{TRequest, TResponse}"/> handler-info entries.</summary>
        public readonly List<PipelineBehaviorHandlerInfo> AllPipelineBehaviorHandlers = [];

        /// <summary>Registered <see cref="IStreamRequestHandler{TRequest, TResponse}"/> handler-info entries.</summary>
        public readonly List<StreamRequestHandlerInfo> AllStreamHandlers = [];

        /// <summary>Registered <see cref="IStreamPipelineBehavior{TRequest, TResponse}"/> handler-info entries.</summary>
        public readonly List<StreamPipelineBehaviorHandlerInfo> AllStreamPipelineBehaviorHandlers = [];
    }
}
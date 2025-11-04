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
/// Helper type for adding Mediator services to a service context.
/// </summary>
public static class DependencyInjectionHelper
{
    public delegate void CustomAddCallbackDelegate(
        IServiceContext serviceContext,
        MediatorOptions options,
        RegistrationServiceLifetime serviceLifetime,
        HandlerCollection handlerCollection,
        bool append);

    /// <summary>
    /// Adds Mediator services to the specified service context.
    /// </summary>
    /// <remarks>This variant ignores the <see cref="MediatorOptions.Assemblies"/> option to be more compatible with AOT scenarios.</remarks>
    /// <param name="serviceContext">The service context.</param>
    /// <param name="options">The options.</param>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="append">Whether to append to existing registrations or replace them.</param>
    /// <param name="customCallback">A custom callback to execute at the start during registration.</param>
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
    /// Adds Mediator services to the specified service context.
    /// </summary>
    /// <param name="serviceContext">The service context.</param>
    /// <param name="options">The options.</param>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="append">Whether to append to existing registrations or replace them.</param>
    /// <param name="customCallback">A custom callback to execute at the start during registration.</param>
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

    [RequiresUnreferencedCode("Assembly scanning requires unreferenced code. Use explicit handler registration for AOT compatibility.")]
    public static void ScanAssembly(MediatorOptions options, HandlerCollection handlerCollection, Assembly assembly)
    {
        var scanResult = MediatorAssemblyHelper.ScanAssembly(assembly);

        if (scanResult.RequestHandlerTypes != null)
            for (int j = 0; j < scanResult.RequestHandlerTypes.Count; j++)
                handlerCollection.AllHandlers.Add(scanResult.RequestHandlerTypes[j]);

        if (scanResult.StreamRequestHandlerTypes != null)
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

    private static void AddPipelineBehaviors<TGlobalPipelineInterface, TGlobalPipelineRegistry, THandlerInfo>(
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

        TGlobalPipelineInterface? globalPipelineRegistry = default;
        if (!append || !serviceContext.IsServiceRegistered<TGlobalPipelineInterface>())
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

        IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>? globalNotificationHandlerRegistry = null;

        if (!append || !serviceContext.IsServiceRegistered<IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>>())
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
    }

    public class HandlerCollection
    {
        public readonly List<RequestHandlerInfo> AllHandlers = [];
        public readonly List<StreamRequestHandlerInfo> AllStreamHandlers = [];
        public readonly List<PipelineBehaviorHandlerInfo> AllPipelineBehaviorHandlers = [];
        public readonly List<StreamPipelineBehaviorHandlerInfo> AllStreamPipelineBehaviorHandlers = [];
        public readonly List<NotificationHandlerInfo> AllNotificationHandlers = [];
    }
}

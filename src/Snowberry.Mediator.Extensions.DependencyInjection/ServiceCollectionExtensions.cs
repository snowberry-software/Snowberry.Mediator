using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Models;
using Snowberry.Mediator.Registries;
using Snowberry.Mediator.Registries.Contracts;

namespace Snowberry.Mediator.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Mediator services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration method.</param>
    /// <param name="serviceLifetime">The service lifetime of the mediator and handlers.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddSnowberryMediator(this IServiceCollection services, Action<MediatorOptions> configure, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var options = new MediatorOptions();
        configure(options);

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), serviceLifetime));

        if (options.Assemblies == null || options.Assemblies.Count == 0)
            return services;

        var allHandlers = new List<RequestHandlerInfo>();
        var allStreamHandlers = new List<StreamRequestHandlerInfo>();
        var allPipelineBehaviorHandlers = new List<PipelineBehaviorHandlerInfo>();
        var allStreamPipelineBehaviorHandlers = new List<StreamPipelineBehaviorHandlerInfo>();
        var allNotificationHandlers = new List<NotificationHandlerInfo>();

        for (int i = 0; i < options.Assemblies.Count; i++)
        {
            var assembly = options.Assemblies[i];

            var scanResult = MediatorAssemblyHelper.ScanAssembly(assembly);

            if (scanResult.RequestHandlerTypes != null)
                for (int j = 0; j < scanResult.RequestHandlerTypes.Count; j++)
                    allHandlers.Add(scanResult.RequestHandlerTypes[j]);

            if (scanResult.StreamRequestHandlerTypes != null)
                for (int j = 0; j < scanResult.StreamRequestHandlerTypes.Count; j++)
                    allStreamHandlers.Add(scanResult.StreamRequestHandlerTypes[j]);

            if (options.RegisterPipelineBehaviors && options.ScanPipelineBehaviors && scanResult.PipelineBehaviorTypes != null)
                for (int j = 0; j < scanResult.PipelineBehaviorTypes.Count; j++)
                    allPipelineBehaviorHandlers.Add(scanResult.PipelineBehaviorTypes[j]);

            if (options.RegisterStreamPipelineBehaviors && options.ScanStreamPipelineBehaviors && scanResult.StreamPipelineBehaviorTypes != null)
                for (int j = 0; j < scanResult.StreamPipelineBehaviorTypes.Count; j++)
                    allStreamPipelineBehaviorHandlers.Add(scanResult.StreamPipelineBehaviorTypes[j]);

            if (options.RegisterNotificationHandlers && options.ScanNotificationHandlers && scanResult.NotificationHandlerTypes != null)
                for (int j = 0; j < scanResult.NotificationHandlerTypes.Count; j++)
                    allNotificationHandlers.Add(scanResult.NotificationHandlerTypes[j]);
        }

        var pipelineBehaviorType = typeof(IPipelineBehavior<,>);
        var streamPipelineBehaviorType = typeof(IStreamPipelineBehavior<,>);

        if (options.PipelineBehaviorTypes != null)
            MediatorAssemblyHelper.ParsePipelineBehaviors(pipelineBehaviorType, options.PipelineBehaviorTypes, allPipelineBehaviorHandlers);

        if (options.StreamPipelineBehaviorTypes != null)
            MediatorAssemblyHelper.ParsePipelineBehaviors(streamPipelineBehaviorType, options.StreamPipelineBehaviorTypes, allStreamPipelineBehaviorHandlers);

        if (options.NotificationHandlerTypes != null)
            MediatorAssemblyHelper.ParseNotificationHandlers(options.NotificationHandlerTypes, allNotificationHandlers);

        for (int i = 0; i < allHandlers.Count; i++)
        {
            var handlerInfo = allHandlers[i];
            services.TryAdd(new ServiceDescriptor(handlerInfo.CreateRequestHandlerInterfaceType(), handlerInfo.HandlerType, serviceLifetime));
        }

        for (int i = 0; i < allStreamHandlers.Count; i++)
        {
            var handlerInfo = allStreamHandlers[i];
            services.TryAdd(new ServiceDescriptor(handlerInfo.CreateStreamRequestHandlerInterfaceType(), handlerInfo.HandlerType, serviceLifetime));
        }

        if (options.RegisterPipelineBehaviors && allPipelineBehaviorHandlers.Count > 0)
            AddPipelineBehaviors<IGlobalPipelineRegistry, GlobalPipelineRegistry, PipelineBehaviorHandlerInfo>(
                services,
                serviceLifetime,
                allPipelineBehaviorHandlers);

        if (options.RegisterStreamPipelineBehaviors && allStreamPipelineBehaviorHandlers.Count > 0)
            AddPipelineBehaviors<IGlobalStreamPipelineRegistry, GlobalStreamPipelineRegistry, StreamPipelineBehaviorHandlerInfo>(
                services,
                serviceLifetime,
                allStreamPipelineBehaviorHandlers);

        if (options.RegisterNotificationHandlers && allNotificationHandlers.Count > 0)
            AddNotificationHandlers(services, serviceLifetime, allNotificationHandlers);

        return services;
    }

    private static void AddPipelineBehaviors<TGlobalPipelineInterface, TGlobalPipelineRegistry, THandlerInfo>(IServiceCollection services, ServiceLifetime serviceLifetime, IList<THandlerInfo> pipelineBehaviorHandlers)
        where TGlobalPipelineRegistry : IBaseGlobalPipelineRegistry<THandlerInfo>, new()
        where THandlerInfo : PipelineBehaviorHandlerInfo
    {
        if (pipelineBehaviorHandlers.Count == 0)
            return;

        var globalPipelineRegistry = new TGlobalPipelineRegistry();
        services.TryAdd(new ServiceDescriptor(serviceType: typeof(TGlobalPipelineInterface), instance: globalPipelineRegistry));

        for (int i = 0; i < pipelineBehaviorHandlers.Count; i++)
        {
            var handler = pipelineBehaviorHandlers[i];
            globalPipelineRegistry.Register(handler);

            services.TryAdd(new ServiceDescriptor(handler.HandlerType, handler.HandlerType, serviceLifetime));
        }
    }

    private static void AddNotificationHandlers(IServiceCollection services, ServiceLifetime serviceLifetime, IList<NotificationHandlerInfo> notificationHandlers)
    {
        if (notificationHandlers.Count == 0)
            return;

        var globalNotificationHandlerRegistry = new GlobalNotificationHandlerRegistry();
        services.TryAdd(new ServiceDescriptor(serviceType: typeof(IGlobalNotificationHandlerRegistry<NotificationHandlerInfo>), instance: globalNotificationHandlerRegistry));

        for (int i = 0; i < notificationHandlers.Count; i++)
        {
            var handler = notificationHandlers[i];
            globalNotificationHandlerRegistry.Register(handler);

            services.TryAdd(new ServiceDescriptor(handler.HandlerType, handler.HandlerType, serviceLifetime));
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;
using Snowberry.Mediator.Models;

namespace Snowberry.Mediator.Extensions;

public static class MediatorAssemblyHelper
{
    /// <summary>
    /// Scans the provided assembly for mediator contracts.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The result.</returns>
    [RequiresUnreferencedCode("Assembly scanning requires unreferenced code. Use explicit handler registration for AOT compatibility.")]
    public static AssemblyScanResult ScanAssembly(Assembly assembly)
    {
        List<Type>? requestTypes = null;
        List<RequestHandlerInfo>? requestHandlers = null;
        List<Type>? streamRequestTypes = null;
        List<StreamRequestHandlerInfo>? streamRequestHandlers = null;
        List<Type>? notificationTypes = null;
        List<NotificationHandlerInfo>? notificationHandlers = null;
        List<PipelineBehaviorHandlerInfo>? pipelineBehaviorHandlers = null;
        List<StreamPipelineBehaviorHandlerInfo>? streamPipelineBehaviorHandlers = null;

        var requestWithResponse = typeof(IRequest<,>);
        var requestHandler = typeof(IRequestHandler<,>);
        var streamRequestWithResponse = typeof(IStreamRequest<,>);
        var streamRequestHandler = typeof(IStreamRequestHandler<,>);
        var notificationMarker = typeof(INotification);
        var notificationHandler = typeof(INotificationHandler<>);
        var pipelineBehavior = typeof(IPipelineBehavior<,>);
        var streamPipelineBehavior = typeof(IStreamPipelineBehavior<,>);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var interfaces = type.GetInterfaces();

            if (interfaces.Length == 0)
                continue;

            for (int i = 0; i < interfaces.Length; i++)
            {
                var inter = interfaces[i];

                // Marker / non-generic cases
                if (inter == notificationMarker)
                {
                    (notificationTypes ??= []).Add(type);
                    continue;
                }

                if (!inter.IsGenericType)
                    continue;

                var def = inter.GetGenericTypeDefinition();
                if (def == requestWithResponse)
                {
                    (requestTypes ??= []).Add(type);
                }
                else if (def == streamRequestWithResponse)
                {
                    (streamRequestTypes ??= []).Add(type);
                }
                else if (def == requestHandler)
                {
                    var genericArguments = inter.GetGenericArguments();
                    var requestType = genericArguments[0];
                    var responseType = genericArguments[1];

                    (requestHandlers ??= []).Add(new()
                    {
                        HandlerType = type,
                        RequestType = requestType,
                        ResponseType = responseType
                    });
                }
                else if (def == streamRequestHandler)
                {
                    var genericArguments = inter.GetGenericArguments();
                    var requestType = genericArguments[0];
                    var responseType = genericArguments[1];

                    (streamRequestHandlers ??= []).Add(new()
                    {
                        HandlerType = type,
                        RequestType = requestType,
                        ResponseType = responseType
                    });
                }
                else if (def == notificationHandler)
                {
                    var genericArguments = inter.GetGenericArguments();
                    var notificationType = genericArguments[0];

                    (notificationHandlers ??= []).Add(new()
                    {
                        HandlerType = type,
                        NotificationType = notificationType
                    });
                }
                else if (def == pipelineBehavior)
                {
                    var genericArguments = inter.GetGenericArguments();
                    var requestType = genericArguments[0];
                    var responseType = genericArguments[1];

                    (pipelineBehaviorHandlers ??= []).Add(new()
                    {
                        HandlerType = type,
                        RequestType = requestType,
                        ResponseType = responseType
                    });
                }
                else if (def == streamPipelineBehavior)
                {
                    var genericArguments = inter.GetGenericArguments();
                    var requestType = genericArguments[0];
                    var responseType = genericArguments[1];

                    (streamPipelineBehaviorHandlers ??= []).Add(new()
                    {
                        HandlerType = type,
                        RequestType = requestType,
                        ResponseType = responseType
                    });
                }
            }
        }

        return new AssemblyScanResult()
        {
            RequestTypes = requestTypes?.AsReadOnly(),
            RequestHandlerTypes = requestHandlers?.AsReadOnly(),
            NotificationHandlerTypes = notificationHandlers?.AsReadOnly(),
            NotificationTypes = notificationTypes?.AsReadOnly(),
            StreamRequestHandlerTypes = streamRequestHandlers?.AsReadOnly(),
            StreamRequestTypes = streamRequestTypes?.AsReadOnly(),
            PipelineBehaviorTypes = pipelineBehaviorHandlers?.AsReadOnly(),
            StreamPipelineBehaviorTypes = streamPipelineBehaviorHandlers?.AsReadOnly()
        };
    }

    [RequiresDynamicCode("Creating generic types at runtime requires dynamic code. Use explicit handler registration for AOT compatibility.")]
    public static Type CreateRequestHandlerInterfaceType(this RequestHandlerInfo requestHandlerInfo)
    {
        return typeof(IRequestHandler<,>).MakeGenericType(requestHandlerInfo.RequestType, requestHandlerInfo.ResponseType);
    }

    [RequiresDynamicCode("Creating generic types at runtime requires dynamic code. Use explicit handler registration for AOT compatibility.")]
    public static Type CreateStreamRequestHandlerInterfaceType(this StreamRequestHandlerInfo streamRequestHandlerInfo)
    {
        return typeof(IStreamRequestHandler<,>).MakeGenericType(streamRequestHandlerInfo.RequestType, streamRequestHandlerInfo.ResponseType);
    }

    /// <summary>
    /// Parses handler info for request and stream request handlers.
    /// </summary>
    /// <param name="handlerInterfaceType">The handler interface type (e.g., IRequestHandler<,> or IStreamRequestHandler<,>).</param>
    /// <param name="collection">The collection of handler types to parse.</param>
    /// <param name="target">The target collection to add parsed handlers to.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Handler types are explicitly registered by the user, not discovered through reflection.")]
    public static void ParseHandlerInfo<THandlerInfo>(Type handlerInterfaceType, List<Type> collection, List<THandlerInfo> target)
        where THandlerInfo : RequestHandlerInfo, new()
    {
        for (int i = 0; i < collection.Count; i++)
        {
            var type = collection[i];
            var parsed = RequestHandlerInfo.TryParse<THandlerInfo>(type, handlerInterfaceType);

            if (parsed != null)
                for (int j = 0; j < parsed.Count; j++)
                    target.Add(parsed[j]);
        }
    }

    /// <summary>
    /// Parses handler info for notification handlers.
    /// </summary>
    /// <param name="collection">The collection of handler types to parse.</param>
    /// <param name="target">The target collection to add parsed handlers to.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Handler types are explicitly registered by the user, not discovered through reflection.")]
    public static void ParseNotificationHandlers(List<Type> collection, List<NotificationHandlerInfo> target)
    {
        for (int i = 0; i < collection.Count; i++)
        {
            var type = collection[i];
            var parsed = NotificationHandlerInfo.TryParse(type);

            if (parsed != null)
                for (int j = 0; j < parsed.Count; j++)
                    target.Add(parsed[j]);
        }
    }
}

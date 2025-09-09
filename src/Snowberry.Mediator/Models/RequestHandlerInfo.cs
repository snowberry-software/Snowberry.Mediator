namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a request handler.
/// </summary>
public class RequestHandlerInfo : IEquatable<RequestHandlerInfo>
{
    public static IList<T>? TryParse<T>(Type type, Type expectedInterface) where T : RequestHandlerInfo, new()
    {
        if (type.IsAbstract || type.IsInterface)
            return default;

        var interfaces = type.GetInterfaces();

        var results = new List<T>();
        for (int i = 0; i < interfaces.Length; i++)
        {
            var inter = interfaces[i];

            if (!inter.IsGenericType)
                continue;

            var def = inter.GetGenericTypeDefinition();

            if (def == expectedInterface)
            {
                var genericArguments = inter.GetGenericArguments();
                var requestType = genericArguments[0];
                var responseType = genericArguments[1];

                results.Add(new T()
                {
                    HandlerType = type,
                    RequestType = requestType,
                    ResponseType = responseType
                });
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{HandlerType.FullName} : {RequestType.FullName} -> {ResponseType.FullName}";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is RequestHandlerInfo handlerInfo)
            return Equals(handlerInfo);

        return base.Equals(obj);
    }

    /// <inheritdoc/>
    public bool Equals(RequestHandlerInfo? other)
    {
        if (other is null)
            return false;

        return other.HandlerType == HandlerType
            && other.RequestType == RequestType
            && other.ResponseType == ResponseType;
    }

    /// <summary>
    /// The request type.
    /// </summary>
    public Type RequestType { get; init; } = null!;

    /// <summary>
    /// The response type.
    /// </summary>
    public Type ResponseType { get; init; } = null!;

    /// <summary>
    /// The handler type.
    /// </summary>
    public Type HandlerType { get; init; } = null!;
}

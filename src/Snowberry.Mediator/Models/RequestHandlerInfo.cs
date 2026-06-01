using System.Diagnostics.CodeAnalysis;

namespace Snowberry.Mediator.Models;

/// <summary>
/// Handler information for a request handler.
/// </summary>
public class RequestHandlerInfo : IEquatable<RequestHandlerInfo>
{
    /// <summary>
    /// Parses a handler type into one <typeparamref name="T"/> per implementation of
    /// <paramref name="expectedInterface"/> it declares, extracting the request and response types.
    /// </summary>
    /// <typeparam name="T">The handler-info type to produce.</typeparam>
    /// <param name="type">The handler type to inspect.</param>
    /// <param name="expectedInterface">The open-generic handler interface to match (for example, <c>IRequestHandler&lt;,&gt;</c>).</param>
    /// <returns>The parsed handler-info entries, or <see langword="null"/> if <paramref name="type"/> is abstract or an interface.</returns>
    public static IList<T>? TryParse<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, Type expectedInterface) where T : RequestHandlerInfo, new()
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

            if (def != expectedInterface)
                continue;

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

        return results;
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

    /// <inheritdoc/>
    public override int GetHashCode()
    {
#if NET9_0_OR_GREATER
        return HashCode.Combine(HandlerType, RequestType, ResponseType);
#else
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + (HandlerType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (RequestType?.GetHashCode() ?? 0);
            hash = (hash * 31) + (ResponseType?.GetHashCode() ?? 0);
            return hash;
        }
#endif
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{HandlerType.FullName} : {RequestType.FullName} -> {ResponseType.FullName}";
    }

    /// <summary>
    /// The handler type.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    public Type HandlerType { get; init; } = null!;

    /// <summary>
    /// The request type.
    /// </summary>
    public Type RequestType { get; init; } = null!;

    /// <summary>
    /// The response type.
    /// </summary>
    public Type ResponseType { get; init; } = null!;
}
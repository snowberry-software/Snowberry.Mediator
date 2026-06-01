using System.Threading;
using System.Threading.Tasks;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

namespace Sample.Handlers;

/// <summary>A request that adds two numbers (discovered cross-assembly from the console project).</summary>
/// <param name="A">The first addend.</param>
/// <param name="B">The second addend.</param>
public sealed record AddNumbers(int A, int B) : IRequest<AddNumbers, int>;

/// <summary>Handles <see cref="AddNumbers"/> by returning the sum.</summary>
public sealed class AddNumbersHandler : IRequestHandler<AddNumbers, int>
{
    /// <inheritdoc/>
    public ValueTask<int> HandleAsync(AddNumbers request, CancellationToken cancellationToken = default)
        => new(request.A + request.B);
}

/// <summary>A notification raised when an item is created.</summary>
/// <param name="Name">The created item's name.</param>
public sealed record ItemCreated(string Name) : INotification;

/// <summary>Writes a line when an <see cref="ItemCreated"/> notification is published.</summary>
public sealed class ItemCreatedHandler : INotificationHandler<ItemCreated>
{
    /// <inheritdoc/>
    public ValueTask HandleAsync(ItemCreated notification, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"  item created: {notification.Name}");
        return default;
    }
}

/// <summary>
/// Open-generic pipeline behavior that logs before and after each request; closed per discovered request
/// type by the generator for AOT-clean dispatch.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
    {
        System.Console.WriteLine($"  -> handling {typeof(TRequest).Name}");
        var response = await next.InvokeAsync(request, cancellationToken);
        System.Console.WriteLine($"  <- handled {typeof(TRequest).Name}");
        return response;
    }
}

/// <summary>Open-generic notification handler that audits every notification; flattened to closed registrations by the generator.</summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public sealed class AuditNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <inheritdoc/>
    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"  [audit] {typeof(TNotification).Name}");
        return default;
    }
}

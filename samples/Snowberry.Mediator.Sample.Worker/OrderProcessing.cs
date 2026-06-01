using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.Abstractions.Pipeline;

namespace Snowberry.Mediator.Sample.Worker;

/// <summary>A request to process an order and return its total price.</summary>
/// <param name="OrderId">The identifier of the order being processed.</param>
/// <param name="Quantity">The number of items in the order.</param>
public sealed record ProcessOrder(int OrderId, int Quantity) : IRequest<ProcessOrder, decimal>;

/// <summary>Handles <see cref="ProcessOrder"/> by computing the order total.</summary>
public sealed class ProcessOrderHandler : IRequestHandler<ProcessOrder, decimal>
{
    private const decimal UnitPrice = 19.99m;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException"><paramref name="request"/> has no items to process.</exception>
    public async ValueTask<decimal> HandleAsync(ProcessOrder request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new InvalidOperationException($"Order {request.OrderId} has no items to process.");

        // Simulate I/O-bound work so the dispatch span has a measurable duration in the dashboard.
        await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken);

        return request.Quantity * UnitPrice;
    }
}

/// <summary>A notification raised once an order has been processed.</summary>
/// <param name="OrderId">The identifier of the processed order.</param>
/// <param name="Total">The computed total price of the order.</param>
public sealed record OrderProcessed(int OrderId, decimal Total) : INotification;

/// <summary>Records an audit entry when an <see cref="OrderProcessed"/> notification is published.</summary>
/// <param name="logger">The logger used to write the audit entry.</param>
public sealed class OrderAuditHandler(ILogger<OrderAuditHandler> logger) : INotificationHandler<OrderProcessed>
{
    /// <inheritdoc/>
    public ValueTask HandleAsync(OrderProcessed notification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Audit: order {OrderId} recorded with total {Total:C}", notification.OrderId, notification.Total);
        return default;
    }
}

/// <summary>Simulates dispatching a confirmation email when an <see cref="OrderProcessed"/> notification is published.</summary>
/// <param name="logger">The logger used to report the confirmation.</param>
public sealed class OrderConfirmationEmailHandler(ILogger<OrderConfirmationEmailHandler> logger) : INotificationHandler<OrderProcessed>
{
    /// <inheritdoc/>
    public async ValueTask HandleAsync(OrderProcessed notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken);
        logger.LogInformation("Confirmation email queued for order {OrderId}", notification.OrderId);
    }
}

/// <summary>
/// Open-generic pipeline behavior that logs before and after each request; closed per discovered request
/// type by the generator for AOT-clean dispatch.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="logger">The logger used to trace the request boundary.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    /// <inheritdoc/>
    public async ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
        where TNext : struct, IPipelineContinuation<TRequest, TResponse>
    {
        logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next.InvokeAsync(request, cancellationToken);
        logger.LogDebug("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

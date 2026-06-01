using Snowberry.Mediator.Abstractions;

namespace Snowberry.Mediator.Sample.Worker;

/// <summary>
/// A background service that periodically dispatches orders through the mediator so the
/// OpenTelemetry instrumentation produces a continuous stream of traces and metrics in the
/// Aspire dashboard. A share of orders intentionally fail to demonstrate error spans.
/// </summary>
/// <param name="scopeFactory">The factory used to create a dependency-injection scope per dispatch.</param>
/// <param name="logger">The logger used to report processed and failed orders.</param>
public sealed class OrderWorker(IServiceScopeFactory scopeFactory, ILogger<OrderWorker> logger) : BackgroundService
{
    private static readonly TimeSpan s_Interval = TimeSpan.FromSeconds(2);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            // The mediator and its handlers are scoped, so each dispatch runs in its own scope, mirroring
            // how a request would be handled in a web application.
            using (var scope = scopeFactory.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                int orderId = random.Next(1000, 9999);
                int quantity = random.Next(0, 5);

                try
                {
                    decimal total = await mediator.SendAsync<ProcessOrder, decimal>(new ProcessOrder(orderId, quantity), stoppingToken);
                    logger.LogInformation("Processed order {OrderId} ({Quantity} items): {Total:C}", orderId, quantity, total);

                    await mediator.PublishAsync(new OrderProcessed(orderId, total), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Order {OrderId} could not be processed", orderId);
                }
            }

            try
            {
                await Task.Delay(s_Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

using System.Collections.Concurrent;
using Snowberry.DependencyInjection;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Comprehensive tests for notification handling, including concrete and open generic handlers
/// </summary>
public class Snowberry_NotificationTests : Common.MediatorTestBase
{
    [Fact]
    public async Task Test_SimpleNotification_SingleConcreteHandler()
    {
        // Clean up static state
        SimpleNotificationHandler.ClearReceivedNotifications();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Only register specific concrete handlers
            options.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification
        {
            Message = "Test notification",
            Value = 42
        };

        await mediator.PublishAsync(notification, CancellationToken.None);

        // Verify only the registered handler was executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Single(executions);
        Assert.Contains(nameof(SimpleNotificationHandler), executions);

        // Verify notification was received
        Assert.Single(SimpleNotificationHandler.ReceivedNotifications);
        var receivedNotification = SimpleNotificationHandler.ReceivedNotifications.First();
        Assert.Equal(notification.Message, receivedNotification.Message);
        Assert.Equal(notification.Value, receivedNotification.Value);
    }

    [Fact]
    public async Task Test_SimpleNotification_MultipleConcreteHandlers()
    {
        // Clean up static state
        SimpleNotificationHandler.ClearReceivedNotifications();
        AnotherSimpleNotificationHandler.ResetExecutionCount();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register multiple concrete handlers explicitly
            options.NotificationHandlerTypes = [
                typeof(SimpleNotificationHandler),
                typeof(AnotherSimpleNotificationHandler)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification
        {
            Message = "Multiple handlers test",
            Value = 123
        };

        await mediator.PublishAsync(notification, CancellationToken.None);

        // Verify both registered handlers were executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.Contains(nameof(SimpleNotificationHandler), executions);
        Assert.Contains(nameof(AnotherSimpleNotificationHandler), executions);

        // Verify each handler processed the notification
        Assert.Single(SimpleNotificationHandler.ReceivedNotifications);
        Assert.Equal(1, AnotherSimpleNotificationHandler.ExecutionCount);

        var receivedNotification = SimpleNotificationHandler.ReceivedNotifications.First();
        Assert.Equal(notification.Message, receivedNotification.Message);
        Assert.Equal(notification.Value, receivedNotification.Value);
    }

    [Fact]
    public async Task Test_UserRegisteredNotification_MultipleConcreteHandlers()
    {
        // Clean up static state
        UserRegisteredNotificationHandler.ClearProcessedUsers();
        UserRegistrationEmailHandler.ClearEmailsSent();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(UserRegisteredNotification).Assembly];
            // Register specific domain handlers
            options.NotificationHandlerTypes = [
                typeof(UserRegisteredNotificationHandler),
                typeof(UserRegistrationEmailHandler)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new UserRegisteredNotification
        {
            UserId = "user123",
            Email = "test@example.com",
            Name = "John Doe"
        };

        await mediator.PublishAsync(notification, CancellationToken.None);

        // Verify both domain-specific handlers executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.Contains(nameof(UserRegisteredNotificationHandler), executions);
        Assert.Contains(nameof(UserRegistrationEmailHandler), executions);

        // Verify business logic was executed
        Assert.Single(UserRegisteredNotificationHandler.ProcessedUsers);
        Assert.Single(UserRegistrationEmailHandler.EmailsSent);

        var processedUser = UserRegisteredNotificationHandler.ProcessedUsers.First();
        Assert.Equal(notification.UserId, processedUser.UserId);
        Assert.Equal(notification.Email, processedUser.Email);
        Assert.Equal(notification.Name, processedUser.Name);

        string emailSent = UserRegistrationEmailHandler.EmailsSent.First();
        Assert.Contains(notification.Email, emailSent);
    }

    [Fact]
    public async Task Test_OpenGenericHandlers_SingleNotification()
    {
        // Clean up static state
        GenericLoggingHandler<SimpleNotification>.ClearLoggedNotifications();
        GenericAuditingHandler<SimpleNotification>.ClearAuditLog();
        GenericMetricsHandler<SimpleNotification>.ClearMetrics();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register only open generic notification handlers
            options.NotificationHandlerTypes = [
                typeof(GenericLoggingHandler<>),
                typeof(GenericAuditingHandler<>),
                typeof(GenericMetricsHandler<>)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification
        {
            Message = "Generic handlers test",
            Value = 999
        };

        await mediator.PublishAsync(notification, CancellationToken.None);

        // Should only execute the 3 registered generic handlers
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(3, executions.Count);

        // Verify generic handlers executed
        Assert.Contains("GenericLoggingHandler<SimpleNotification>", executions);
        Assert.Contains("GenericAuditingHandler<SimpleNotification>", executions);
        Assert.Contains("GenericMetricsHandler<SimpleNotification>", executions);

        // Verify no concrete handlers executed (they weren't registered)
        Assert.DoesNotContain(nameof(SimpleNotificationHandler), executions);
        Assert.DoesNotContain(nameof(AnotherSimpleNotificationHandler), executions);

        // Verify each generic handler processed the notification correctly
        Assert.Single(GenericLoggingHandler<SimpleNotification>.LoggedNotifications);
        Assert.Contains("SimpleNotification", GenericAuditingHandler<SimpleNotification>.AuditLog.Keys);
        Assert.Single(GenericAuditingHandler<SimpleNotification>.AuditLog["SimpleNotification"]);
        Assert.Equal(1, GenericMetricsHandler<SimpleNotification>.NotificationCounts["SimpleNotification"]);
        Assert.True(GenericMetricsHandler<SimpleNotification>.LastProcessedTimes.ContainsKey("SimpleNotification"));
    }

    [Fact]
    public async Task Test_MixedConcreteAndGenericHandlers()
    {
        // Clean up static state
        SimpleNotificationHandler.ClearReceivedNotifications();
        AnotherSimpleNotificationHandler.ResetExecutionCount();
        GenericLoggingHandler<SimpleNotification>.ClearLoggedNotifications();
        GenericMetricsHandler<SimpleNotification>.ClearMetrics();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register mixed handler types explicitly
            options.NotificationHandlerTypes = [
                // Concrete handlers
                typeof(SimpleNotificationHandler),
                typeof(AnotherSimpleNotificationHandler),
                // Open generic handlers
                typeof(GenericLoggingHandler<>),
                typeof(GenericMetricsHandler<>)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification
        {
            Message = "Mixed handlers test",
            Value = 555
        };

        await mediator.PublishAsync(notification, CancellationToken.None);

        // Should have executed 4 handlers total: 2 concrete + 2 generic
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(4, executions.Count);

        // Verify concrete handlers executed
        Assert.Contains(nameof(SimpleNotificationHandler), executions);
        Assert.Contains(nameof(AnotherSimpleNotificationHandler), executions);

        // Verify generic handlers executed
        Assert.Contains("GenericLoggingHandler<SimpleNotification>", executions);
        Assert.Contains("GenericMetricsHandler<SimpleNotification>", executions);

        // Verify all handlers processed the notification
        Assert.Single(SimpleNotificationHandler.ReceivedNotifications);
        Assert.Equal(1, AnotherSimpleNotificationHandler.ExecutionCount);
        Assert.Single(GenericLoggingHandler<SimpleNotification>.LoggedNotifications);
        Assert.Equal(1, GenericMetricsHandler<SimpleNotification>.NotificationCounts["SimpleNotification"]);
    }

    [Fact]
    public async Task Test_MultipleNotificationTypes_GenericHandlers()
    {
        // Clean up static state
        GenericLoggingHandler<UserRegisteredNotification>.ClearLoggedNotifications();
        GenericLoggingHandler<OrderCompletedNotification>.ClearLoggedNotifications();
        GenericMetricsHandler<UserRegisteredNotification>.ClearMetrics();
        GenericMetricsHandler<OrderCompletedNotification>.ClearMetrics();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(UserRegisteredNotification).Assembly];
            // Register only generic handlers to work with multiple notification types
            options.NotificationHandlerTypes = [
                typeof(GenericLoggingHandler<>),
                typeof(GenericMetricsHandler<>)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        // Send different notification types
        var userNotification = new UserRegisteredNotification
        {
            UserId = "user456",
            Email = "another@example.com",
            Name = "Jane Smith"
        };

        var orderNotification = new OrderCompletedNotification
        {
            OrderId = "order789",
            Amount = 99.99m,
            CustomerId = "customer123"
        };

        await mediator.PublishAsync(userNotification, CancellationToken.None);
        await mediator.PublishAsync(orderNotification, CancellationToken.None);

        // Verify generic handlers processed both notification types
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(4, executions.Count); // 2 notifications × 2 generic handlers each

        Assert.Contains("GenericLoggingHandler<UserRegisteredNotification>", executions);
        Assert.Contains("GenericLoggingHandler<OrderCompletedNotification>", executions);
        Assert.Contains("GenericMetricsHandler<UserRegisteredNotification>", executions);
        Assert.Contains("GenericMetricsHandler<OrderCompletedNotification>", executions);

        // Verify no concrete handlers executed (they weren't registered)
        Assert.DoesNotContain(nameof(UserRegisteredNotificationHandler), executions);
        Assert.DoesNotContain(nameof(UserRegistrationEmailHandler), executions);
        Assert.DoesNotContain(nameof(OrderCompletionHandler), executions);
    }

    [Fact]
    public async Task Test_OnlyGenericHandlers_MultipleTypes()
    {
        // Clean up static state
        GenericLoggingHandler<SimpleNotification>.ClearLoggedNotifications();
        GenericLoggingHandler<SystemEventNotification>.ClearLoggedNotifications();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register only one generic handler type
            options.NotificationHandlerTypes = [typeof(GenericLoggingHandler<>)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var simpleNotification = new SimpleNotification { Message = "Only generic test", Value = 1 };
        var systemNotification = new SystemEventNotification { EventType = "Test", Description = "Generic only" };

        await mediator.PublishAsync(simpleNotification, CancellationToken.None);
        await mediator.PublishAsync(systemNotification, CancellationToken.None);

        // Should only execute generic handlers
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.Contains("GenericLoggingHandler<SimpleNotification>", executions);
        Assert.Contains("GenericLoggingHandler<SystemEventNotification>", executions);

        // Verify no concrete handlers executed
        Assert.DoesNotContain(nameof(SimpleNotificationHandler), executions);
        Assert.DoesNotContain(nameof(SystemEventLoggingHandler), executions);
    }

    [Fact]
    public async Task Test_NotificationHandler_ExceptionHandling()
    {
        // Initialize clean test context
        TestSpecificValidationHandler<SimpleNotification>.ClearValidationResults();
        TestSpecificValidationHandler<SimpleNotification>.ShouldThrowException = true;

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Only register the exception-throwing handler
            options.NotificationHandlerTypes = [typeof(TestSpecificValidationHandler<>)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification
        {
            Message = "Exception test",
            Value = 0
        };

        // Verify the exception flag is set before the test
        Assert.True(TestSpecificValidationHandler<SimpleNotification>.ShouldThrowException);

        // Should propagate exception from handler
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.PublishAsync(notification, CancellationToken.None);
        });

        // Verify the exception message
        Assert.Contains("Validation failed for SimpleNotification", exception.Message);

        // Verify handler was called before exception
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Single(executions);
        Assert.Contains("TestSpecificValidationHandler<SimpleNotification>", executions);

        // Clean up for this test context
        TestSpecificValidationHandler<SimpleNotification>.ClearValidationResults();
    }

    [Fact]
    public async Task Test_NotificationHandler_NoRaceConditions_ConcurrentExceptionTests()
    {
        // Test to verify that concurrent exception tests don't interfere with each other
        var tasks = new List<Task>();
        var exceptions = new ConcurrentBag<Exception>();
        int successCount = 0;

        // Run multiple concurrent tests that should all throw exceptions
        for (int i = 0; i < 10; i++)
        {
            int testIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Each task gets its own isolated context
                    TestSpecificValidationHandler<SimpleNotification>.ClearValidationResults();
                    TestSpecificValidationHandler<SimpleNotification>.ShouldThrowException = true;

                    using var serviceContainer = new ServiceContainer();
                    serviceContainer.AddSnowberryMediator(options =>
                    {
                        options.Assemblies = [typeof(SimpleNotification).Assembly];
                        options.NotificationHandlerTypes = [typeof(TestSpecificValidationHandler<>)];
                    }, serviceLifetime: ServiceLifetime.Scoped);

                    var mediator = serviceContainer.GetRequiredService<IMediator>();

                    var notification = new SimpleNotification
                    {
                        Message = $"Concurrent exception test {testIndex}",
                        Value = testIndex
                    };

                    // This should throw
                    await mediator.PublishAsync(notification, CancellationToken.None);

                    // If we reach here, no exception was thrown (this is bad)
                    throw new InvalidOperationException($"Test {testIndex}: Expected exception was not thrown!");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Validation failed"))
                {
                    // This is the expected exception - success
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    // Unexpected exception
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Verify all tasks succeeded in throwing the expected exception
        Assert.Equal(10, successCount);
        Assert.Empty(exceptions);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task Test_NotificationHandlers_DifferentServiceLifetimes(ServiceLifetime lifetime)
    {
        // Clean up static state
        SimpleNotificationHandler.ClearReceivedNotifications();
        GenericLoggingHandler<SimpleNotification>.ClearLoggedNotifications();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register one concrete and one generic handler
            options.NotificationHandlerTypes = [
                typeof(SimpleNotificationHandler),
                typeof(GenericLoggingHandler<>)
            ];
        }, serviceLifetime: lifetime);

        // Test multiple notifications to verify lifetime behavior
        for (int i = 0; i < 3; i++)
        {
            using var scope = serviceContainer.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var notification = new SimpleNotification
            {
                Message = $"Lifetime test {i}",
                Value = i
            };

            await mediator.PublishAsync(notification, CancellationToken.None);
        }

        // Should have executed both handler types for each notification
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(6, executions.Count); // 2 handlers × 3 notifications

        // Verify both handler types were called for each notification
        Assert.Equal(3, executions.Count(e => e == nameof(SimpleNotificationHandler)));
        Assert.Equal(3, executions.Count(e => e == "GenericLoggingHandler<SimpleNotification>"));

        // Verify all notifications were processed
        Assert.Equal(3, SimpleNotificationHandler.ReceivedNotifications.Count);
        Assert.Equal(3, GenericLoggingHandler<SimpleNotification>.LoggedNotifications.Count);
    }

    [Fact]
    public async Task Test_ConcurrentNotificationPublishing()
    {
        // Clean up static state
        GenericMetricsHandler<SimpleNotification>.ClearMetrics();
        GenericLoggingHandler<SimpleNotification>.ClearLoggedNotifications();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register only generic handlers for cleaner concurrent test
            options.NotificationHandlerTypes = [
                typeof(GenericMetricsHandler<>),
                typeof(GenericLoggingHandler<>)
            ];
        }, serviceLifetime: ServiceLifetime.Singleton);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var tasks = new List<Task>();

        // Publish 5 notifications concurrently
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                var notification = new SimpleNotification
                {
                    Message = $"Concurrent test {index}",
                    Value = index
                };

                await mediator.PublishAsync(notification, CancellationToken.None);
            }));
        }

        await Task.WhenAll(tasks);

        // With thread-safe collections, we should get exact counts
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(10, executions.Count); // Exactly 2 handlers × 5 notifications

        int metricsCount = executions.Count(e => e == "GenericMetricsHandler<SimpleNotification>");
        int loggingCount = executions.Count(e => e == "GenericLoggingHandler<SimpleNotification>");

        // Each notification should have been processed by both handlers
        Assert.Equal(5, metricsCount);
        Assert.Equal(5, loggingCount);

        // Verify metrics were collected properly with thread-safe collections
        Assert.Equal(5, GenericMetricsHandler<SimpleNotification>.NotificationCounts["SimpleNotification"]);
        Assert.Equal(5, GenericLoggingHandler<SimpleNotification>.LoggedNotifications.Count);
    }

    [Fact]
    public async Task Test_NotificationCancellation()
    {
        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Register one handler for cancellation test
            options.NotificationHandlerTypes = [typeof(SimpleNotificationHandler)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification { Message = "Cancellation test", Value = 0 };

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        // Should respect cancellation token
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await mediator.PublishAsync(notification, cts.Token);
        });
    }

    [Fact]
    public async Task Test_OrderCompletedNotification_BusinessLogic()
    {
        // Clean up static state
        OrderCompletionHandler.Reset();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(OrderCompletedNotification).Assembly];
            // Register only the order completion handler
            options.NotificationHandlerTypes = [typeof(OrderCompletionHandler)];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification1 = new OrderCompletedNotification
        {
            OrderId = "order001",
            Amount = 50.00m,
            CustomerId = "customer123"
        };

        var notification2 = new OrderCompletedNotification
        {
            OrderId = "order002",
            Amount = 75.50m,
            CustomerId = "customer456"
        };

        await mediator.PublishAsync(notification1, CancellationToken.None);
        await mediator.PublishAsync(notification2, CancellationToken.None);

        // Verify handler processed both orders
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.All(executions, execution => Assert.Equal(nameof(OrderCompletionHandler), execution));

        // Verify business logic
        Assert.Equal(2, OrderCompletionHandler.CompletedOrders.Count);
        Assert.Equal(125.50m, OrderCompletionHandler.TotalRevenue);

        Assert.Contains(OrderCompletionHandler.CompletedOrders, order => order.OrderId == "order001");
        Assert.Contains(OrderCompletionHandler.CompletedOrders, order => order.OrderId == "order002");
    }

    [Fact]
    public async Task Test_MultipleNotificationTypes_SeparateHandlers()
    {
        // Clean up static state
        UserRegisteredNotificationHandler.ClearProcessedUsers();
        UserRegistrationEmailHandler.ClearEmailsSent();
        OrderCompletionHandler.Reset();
        SystemEventLoggingHandler.ClearLoggedEvents();

        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(UserRegisteredNotification).Assembly];
            // Register specific handlers for different notification types
            options.NotificationHandlerTypes = [
                typeof(UserRegisteredNotificationHandler),
                typeof(UserRegistrationEmailHandler),
                typeof(OrderCompletionHandler),
                typeof(SystemEventLoggingHandler)
            ];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        // Send different notification types
        var userNotification = new UserRegisteredNotification
        {
            UserId = "user456",
            Email = "test@domain.com",
            Name = "Jane Smith"
        };

        var orderNotification = new OrderCompletedNotification
        {
            OrderId = "order789",
            Amount = 99.99m,
            CustomerId = "customer123"
        };

        var systemNotification = new SystemEventNotification
        {
            EventType = "SystemStartup",
            Description = "Application started successfully"
        };

        await mediator.PublishAsync(userNotification, CancellationToken.None);
        await mediator.PublishAsync(orderNotification, CancellationToken.None);
        await mediator.PublishAsync(systemNotification, CancellationToken.None);

        // Verify appropriate handlers executed for each notification type
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(4, executions.Count); // UserRegistered (2) + Order (1) + System (1)

        // User notification handlers
        Assert.Contains(nameof(UserRegisteredNotificationHandler), executions);
        Assert.Contains(nameof(UserRegistrationEmailHandler), executions);

        // Order notification handler
        Assert.Contains(nameof(OrderCompletionHandler), executions);

        // System event handler
        Assert.Contains(nameof(SystemEventLoggingHandler), executions);

        // Verify each handler processed the correct notification
        Assert.Single(UserRegisteredNotificationHandler.ProcessedUsers);
        Assert.Single(UserRegistrationEmailHandler.EmailsSent);
        Assert.Single(OrderCompletionHandler.CompletedOrders);
        Assert.Single(SystemEventLoggingHandler.LoggedEvents);
    }

    [Fact]
    public async Task Test_NoHandlersRegistered_ThrowsException()
    {
        using var serviceContainer = new ServiceContainer();
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            // Don't register any handlers
            options.NotificationHandlerTypes = [];
        }, serviceLifetime: ServiceLifetime.Scoped);

        var mediator = serviceContainer.GetRequiredService<IMediator>();

        var notification = new SimpleNotification { Message = "No handlers", Value = 0 };

        // Should throw when no handlers are registered for the notification
        await Assert.ThrowsAsync<Abstractions.Exceptions.NotificationHandlerNotFoundException>(async () =>
        {
            await mediator.PublishAsync(notification, CancellationToken.None);
        });
    }
}

/// <summary>
/// Custom exception-throwing handler for testing
/// </summary>
public class Snowberry_ExceptionThrowingNotificationHandler : INotificationHandler<SimpleNotification>
{
    public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Handler intentionally threw an exception");
    }
}
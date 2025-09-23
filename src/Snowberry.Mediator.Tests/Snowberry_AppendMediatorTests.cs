using System.Collections.Concurrent;
using Snowberry.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Handler;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.DependencyInjection;
using Snowberry.Mediator.Tests.Common.Helper;
using Snowberry.Mediator.Tests.Common.NotificationHandlers;
using Snowberry.Mediator.Tests.Common.Notifications;

namespace Snowberry.Mediator.Tests;

/// <summary>
/// Tests for the AppendSnowberryMediator functionality which allows dynamic addition of handlers at runtime
/// </summary>
public class Snowberry_AppendMediatorTests : Common.MediatorTestBase
{
    #region Plugin Test Handlers

    /// <summary>
    /// Plugin notification for testing dynamic registration
    /// </summary>
    public class PluginNotification : INotification
    {
        public string PluginName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    /// <summary>
    /// Core plugin handler that gets registered initially
    /// </summary>
    public class CorePluginHandler : INotificationHandler<PluginNotification>
    {
        public static ConcurrentBag<PluginNotification> ProcessedNotifications =>
            TestIsolationContext.GetOrCreateBag<PluginNotification>("CorePluginHandler.ProcessedNotifications");

        public ValueTask HandleAsync(PluginNotification notification, CancellationToken cancellationToken = default)
        {
            NotificationHandlerExecutionTracker.RecordExecution(nameof(CorePluginHandler));
            ProcessedNotifications.Add(notification);
            return ValueTask.CompletedTask;
        }

        public static void Clear()
        {
            var bag = ProcessedNotifications;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Additional plugin handler that gets appended later
    /// </summary>
    public class AdditionalPluginHandler : INotificationHandler<PluginNotification>
    {
        public static ConcurrentBag<PluginNotification> ProcessedNotifications =>
            TestIsolationContext.GetOrCreateBag<PluginNotification>("AdditionalPluginHandler.ProcessedNotifications");

        public ValueTask HandleAsync(PluginNotification notification, CancellationToken cancellationToken = default)
        {
            NotificationHandlerExecutionTracker.RecordExecution(nameof(AdditionalPluginHandler));
            ProcessedNotifications.Add(notification);
            return ValueTask.CompletedTask;
        }

        public static void Clear()
        {
            var bag = ProcessedNotifications;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Third party plugin handler simulating external module
    /// </summary>
    public class ThirdPartyPluginHandler : INotificationHandler<PluginNotification>
    {
        public static ConcurrentBag<PluginNotification> ProcessedNotifications =>
            TestIsolationContext.GetOrCreateBag<PluginNotification>("ThirdPartyPluginHandler.ProcessedNotifications");

        public ValueTask HandleAsync(PluginNotification notification, CancellationToken cancellationToken = default)
        {
            NotificationHandlerExecutionTracker.RecordExecution(nameof(ThirdPartyPluginHandler));
            ProcessedNotifications.Add(notification);
            return ValueTask.CompletedTask;
        }

        public static void Clear()
        {
            var bag = ProcessedNotifications;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Analytics plugin handler for cross-cutting concerns
    /// </summary>
    public class AnalyticsPluginHandler : INotificationHandler<PluginNotification>
    {
        public static ConcurrentBag<PluginNotification> ProcessedNotifications =>
            TestIsolationContext.GetOrCreateBag<PluginNotification>("AnalyticsPluginHandler.ProcessedNotifications");

        public ValueTask HandleAsync(PluginNotification notification, CancellationToken cancellationToken = default)
        {
            NotificationHandlerExecutionTracker.RecordExecution(nameof(AnalyticsPluginHandler));
            ProcessedNotifications.Add(notification);
            return ValueTask.CompletedTask;
        }

        public static void Clear()
        {
            var bag = ProcessedNotifications;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Request for testing dynamic request handler registration
    /// </summary>
    public class PluginRequest : IRequest<PluginRequest, string>
    {
        public string RequestData { get; set; } = string.Empty;
        public int ProcessingLevel { get; set; }
    }

    /// <summary>
    /// Core request handler
    /// </summary>
    public class CorePluginRequestHandler : IRequestHandler<PluginRequest, string>
    {
        public static ConcurrentBag<PluginRequest> ProcessedRequests =>
            TestIsolationContext.GetOrCreateBag<PluginRequest>("CorePluginRequestHandler.ProcessedRequests");

        public ValueTask<string> HandleAsync(PluginRequest request, CancellationToken cancellationToken = default)
        {
            PipelineExecutionTracker.RecordExecution(nameof(CorePluginRequestHandler));
            ProcessedRequests.Add(request);
            return ValueTask.FromResult($"Core processed: {request.RequestData}");
        }

        public static void Clear()
        {
            var bag = ProcessedRequests;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Enhanced request handler that replaces the core one
    /// </summary>
    public class EnhancedPluginRequestHandler : IRequestHandler<PluginRequest, string>
    {
        public static ConcurrentBag<PluginRequest> ProcessedRequests =>
            TestIsolationContext.GetOrCreateBag<PluginRequest>("EnhancedPluginRequestHandler.ProcessedRequests");

        public ValueTask<string> HandleAsync(PluginRequest request, CancellationToken cancellationToken = default)
        {
            PipelineExecutionTracker.RecordExecution(nameof(EnhancedPluginRequestHandler));
            ProcessedRequests.Add(request);
            return ValueTask.FromResult($"Enhanced processed: {request.RequestData} (Level {request.ProcessingLevel})");
        }

        public static void Clear()
        {
            var bag = ProcessedRequests;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Stream request for testing dynamic stream handler registration
    /// </summary>
    public class PluginStreamRequest : IStreamRequest<PluginStreamRequest, int>
    {
        public string Source { get; set; } = string.Empty;
        public int Count { get; set; }
        public int StartValue { get; set; }
    }

    /// <summary>
    /// Core stream handler
    /// </summary>
    public class CorePluginStreamHandler : IStreamRequestHandler<PluginStreamRequest, int>
    {
        public static ConcurrentBag<PluginStreamRequest> ProcessedRequests =>
            TestIsolationContext.GetOrCreateBag<PluginStreamRequest>("CorePluginStreamHandler.ProcessedRequests");

        public async IAsyncEnumerable<int> HandleAsync(PluginStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            StreamPipelineExecutionTracker.RecordExecution(nameof(CorePluginStreamHandler));
            ProcessedRequests.Add(request);

            for (int i = 0; i < request.Count; i++)
            {
                yield return request.StartValue + i;
                await Task.Delay(1, cancellationToken); // Simulate async work
            }
        }

        public static void Clear()
        {
            var bag = ProcessedRequests;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Enhanced stream handler that multiplies values
    /// </summary>
    public class EnhancedPluginStreamHandler : IStreamRequestHandler<PluginStreamRequest, int>
    {
        public static ConcurrentBag<PluginStreamRequest> ProcessedRequests =>
            TestIsolationContext.GetOrCreateBag<PluginStreamRequest>("EnhancedPluginStreamHandler.ProcessedRequests");

        public async IAsyncEnumerable<int> HandleAsync(PluginStreamRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            StreamPipelineExecutionTracker.RecordExecution(nameof(EnhancedPluginStreamHandler));
            ProcessedRequests.Add(request);

            for (int i = 0; i < request.Count; i++)
            {
                yield return (request.StartValue + i) * 2; // Enhanced: multiply by 2
                await Task.Delay(1, cancellationToken); // Simulate async work
            }
        }

        public static void Clear()
        {
            var bag = ProcessedRequests;
            while (bag.TryTake(out _)) { }
        }
    }

    /// <summary>
    /// Complex request for testing multiple types
    /// </summary>
    public class ComplexPluginRequest : IRequest<ComplexPluginRequest, string>
    {
        public string Data { get; set; } = string.Empty;
        public int ProcessingSteps { get; set; }
    }

    /// <summary>
    /// Complex request handler
    /// </summary>
    public class ComplexPluginRequestHandler : IRequestHandler<ComplexPluginRequest, string>
    {
        public static ConcurrentBag<ComplexPluginRequest> ProcessedRequests =>
            TestIsolationContext.GetOrCreateBag<ComplexPluginRequest>("ComplexPluginRequestHandler.ProcessedRequests");

        public ValueTask<string> HandleAsync(ComplexPluginRequest request, CancellationToken cancellationToken = default)
        {
            PipelineExecutionTracker.RecordExecution(nameof(ComplexPluginRequestHandler));
            ProcessedRequests.Add(request);
            return ValueTask.FromResult($"Complex processed: {request.Data} with {request.ProcessingSteps} steps");
        }

        public static void Clear()
        {
            var bag = ProcessedRequests;
            while (bag.TryTake(out _)) { }
        }
    }

    #endregion

    #region Notification Handler Tests

    [Fact]
    public async Task Test_AppendNotificationHandlers_SinglePlugin()
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true; // Enable notification handler registration
        });

        // Step 2: Simulate plugin loading by appending additional handler
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RegisterNotificationHandlers = true; // Enable notification handler registration
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that both handlers are executed
        var notification = new PluginNotification
        {
            PluginName = "TestPlugin",
            Message = "Hello from plugin",
            Version = 1
        };

        await mediator.PublishAsync(notification);

        // Verify both handlers executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.Contains(nameof(CorePluginHandler), executions);
        Assert.Contains(nameof(AdditionalPluginHandler), executions);

        // Verify both handlers processed the notification
        Assert.Single(CorePluginHandler.ProcessedNotifications);
        Assert.Single(AdditionalPluginHandler.ProcessedNotifications);

        var coreProcessed = CorePluginHandler.ProcessedNotifications.First();
        var additionalProcessed = AdditionalPluginHandler.ProcessedNotifications.First();

        Assert.Equal(notification.PluginName, coreProcessed.PluginName);
        Assert.Equal(notification.PluginName, additionalProcessed.PluginName);
    }

    [Fact]
    public async Task Test_AppendNotificationHandlers_MultiplePlugins()
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        ThirdPartyPluginHandler.Clear();
        AnalyticsPluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Append first plugin
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // Step 3: Append second plugin with multiple handlers
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [
                typeof(ThirdPartyPluginHandler),
                typeof(AnalyticsPluginHandler)
            ];
            options.RegisterNotificationHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 4: Test that all handlers are executed
        var notification = new PluginNotification
        {
            PluginName = "MultiPlugin",
            Message = "Multiple plugins test",
            Version = 2
        };

        await mediator.PublishAsync(notification);

        // Verify all 4 handlers executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(4, executions.Count);
        Assert.Contains(nameof(CorePluginHandler), executions);
        Assert.Contains(nameof(AdditionalPluginHandler), executions);
        Assert.Contains(nameof(ThirdPartyPluginHandler), executions);
        Assert.Contains(nameof(AnalyticsPluginHandler), executions);

        // Verify all handlers processed the notification
        Assert.Single(CorePluginHandler.ProcessedNotifications);
        Assert.Single(AdditionalPluginHandler.ProcessedNotifications);
        Assert.Single(ThirdPartyPluginHandler.ProcessedNotifications);
        Assert.Single(AnalyticsPluginHandler.ProcessedNotifications);
    }

    [Fact]
    public async Task Test_AppendNotificationHandlers_WithExistingNotifications()
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        SimpleNotificationHandler.ClearReceivedNotifications();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with existing handlers
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            options.NotificationHandlerTypes = [
                typeof(SimpleNotificationHandler),
                typeof(CorePluginHandler)
            ];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Append plugin handler
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test existing notification type
        var simpleNotification = new SimpleNotification
        {
            Message = "Existing type test",
            Value = 42
        };

        await mediator.PublishAsync(simpleNotification);

        // Only SimpleNotificationHandler should execute for SimpleNotification
        var simpleExecutions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Single(simpleExecutions);
        Assert.Contains(nameof(SimpleNotificationHandler), simpleExecutions);
        Assert.Single(SimpleNotificationHandler.ReceivedNotifications);

        // Reset tracking for plugin notification test
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        NotificationHandlerExecutionTracker.Clear();

        // Step 4: Test plugin notification type
        var pluginNotification = new PluginNotification
        {
            PluginName = "MixedTest",
            Message = "Mixed handlers test",
            Version = 1
        };

        await mediator.PublishAsync(pluginNotification);

        // Both plugin handlers should execute for PluginNotification
        var pluginExecutions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, pluginExecutions.Count);
        Assert.Contains(nameof(CorePluginHandler), pluginExecutions);
        Assert.Contains(nameof(AdditionalPluginHandler), pluginExecutions);
    }

    [Fact]
    public async Task Test_AppendRequestHandlers_ReplaceExisting()
    {
        // Clean up static state
        CorePluginRequestHandler.Clear();
        EnhancedPluginRequestHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core request handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CorePluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        // Step 2: Append enhanced handler (should replace the core one due to DI behavior)
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(EnhancedPluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that the enhanced handler is used
        var request = new PluginRequest
        {
            RequestData = "Test request",
            ProcessingLevel = 5
        };

        var result = await mediator.SendAsync<PluginRequest, string>(request);

        // Verify the enhanced handler was used
        var executions = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executions);
        Assert.Contains(nameof(EnhancedPluginRequestHandler), executions);

        // Verify the correct result
        Assert.Contains("Enhanced processed", result);
        Assert.Contains("Level 5", result);

        // Verify only the enhanced handler processed the request
        Assert.Empty(CorePluginRequestHandler.ProcessedRequests);
        Assert.Single(EnhancedPluginRequestHandler.ProcessedRequests);
    }

    [Fact]
    public async Task Test_AppendGenericHandlers_WithExisting()
    {
        // Clean up static state
        GenericLoggingHandler<PluginNotification>.ClearLoggedNotifications();
        GenericMetricsHandler<PluginNotification>.ClearMetrics();
        CorePluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with concrete handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Append generic handlers
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [
                typeof(GenericLoggingHandler<>),
                typeof(GenericMetricsHandler<>)
            ];
            options.RegisterNotificationHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that all handlers execute
        var notification = new PluginNotification
        {
            PluginName = "GenericTest",
            Message = "Generic handlers test",
            Version = 3
        };

        await mediator.PublishAsync(notification);

        // Verify all handlers executed (concrete + 2 generic)
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(3, executions.Count);
        Assert.Contains(nameof(CorePluginHandler), executions);
        Assert.Contains("GenericLoggingHandler<PluginNotification>", executions);
        Assert.Contains("GenericMetricsHandler<PluginNotification>", executions);

        // Verify all handlers processed the notification
        Assert.Single(CorePluginHandler.ProcessedNotifications);
        Assert.Single(GenericLoggingHandler<PluginNotification>.LoggedNotifications);
        Assert.Equal(1, GenericMetricsHandler<PluginNotification>.NotificationCounts["PluginNotification"]);
    }

    #endregion

    #region Request Handler Tests

    [Fact]
    public async Task Test_AppendRequestHandlers_ExplicitRegistration()
    {
        // Clean up static state
        CorePluginRequestHandler.Clear();
        EnhancedPluginRequestHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core request handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CorePluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        // Step 2: Append enhanced handler
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(EnhancedPluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that the last registered handler is used (DI container behavior)
        var request = new PluginRequest
        {
            RequestData = "Test request",
            ProcessingLevel = 5
        };

        var result = await mediator.SendAsync<PluginRequest, string>(request);

        // Verify the enhanced handler was used (last registered wins)
        var executions = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executions);
        Assert.Contains(nameof(EnhancedPluginRequestHandler), executions);

        // Verify the correct result
        Assert.Contains("Enhanced processed", result);
        Assert.Contains("Level 5", result);

        // Verify only the enhanced handler processed the request
        Assert.Empty(CorePluginRequestHandler.ProcessedRequests);
        Assert.Single(EnhancedPluginRequestHandler.ProcessedRequests);
    }

    [Fact]
    public async Task Test_AppendRequestHandlers_MultipleTypes()
    {
        // Clean up static state
        CorePluginRequestHandler.Clear();
        ComplexPluginRequestHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CorePluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        // Step 2: Append handler for different request type
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(ComplexPluginRequestHandler)];
            options.RegisterRequestHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that both request types work
        var pluginRequest = new PluginRequest
        {
            RequestData = "Plugin request",
            ProcessingLevel = 1
        };

        var complexRequest = new ComplexPluginRequest
        {
            Data = "Complex request",
            ProcessingSteps = 3
        };

        var pluginResult = await mediator.SendAsync<PluginRequest, string>(pluginRequest);
        var complexResult = await mediator.SendAsync<ComplexPluginRequest, string>(complexRequest);

        // Verify both handlers executed for their respective types
        var executions = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(2, executions.Count);
        Assert.Contains(nameof(CorePluginRequestHandler), executions);
        Assert.Contains(nameof(ComplexPluginRequestHandler), executions);

        // Verify correct results
        Assert.Contains("Core processed", pluginResult);
        Assert.Contains("Complex processed", complexResult);

        // Verify both handlers processed their requests
        Assert.Single(CorePluginRequestHandler.ProcessedRequests);
        Assert.Single(ComplexPluginRequestHandler.ProcessedRequests);
    }

    [Fact]
    public async Task Test_AppendRequestHandlers_EmptyConfiguration()
    {
        // Clean up static state
        CorePluginRequestHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with request handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.RequestHandlerTypes = [typeof(CorePluginRequestHandler)];
        });

        // Step 2: Append with empty configuration (should not break anything)
        serviceContainer.AppendSnowberryMediator(options =>
        {
            // Empty configuration
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that original functionality still works
        var request = new PluginRequest
        {
            RequestData = "Empty append test",
            ProcessingLevel = 1
        };

        var result = await mediator.SendAsync<PluginRequest, string>(request);

        // Verify original handler still works
        var executions = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executions);
        Assert.Contains(nameof(CorePluginRequestHandler), executions);

        Assert.Contains("Core processed", result);
        Assert.Single(CorePluginRequestHandler.ProcessedRequests);
    }

    #endregion

    #region Stream Handler Tests

    [Fact]
    public async Task Test_AppendStreamHandlers_ExplicitRegistration()
    {
        // Clean up static state
        CorePluginStreamHandler.Clear();
        EnhancedPluginStreamHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core stream handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.StreamRequestHandlerTypes = [typeof(CorePluginStreamHandler)];
            options.RegisterStreamRequestHandlers = true;
        });

        // Step 2: Append enhanced stream handler
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.StreamRequestHandlerTypes = [typeof(EnhancedPluginStreamHandler)];
            options.RegisterStreamRequestHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that the last registered handler is used
        var request = new PluginStreamRequest
        {
            Source = "Stream test",
            Count = 3,
            StartValue = 10
        };

        var results = new List<int>();
        await foreach (var item in mediator.CreateStreamAsync<PluginStreamRequest, int>(request))
        {
            results.Add(item);
        }

        // Verify enhanced handler was used (values should be doubled)
        Assert.Equal(3, results.Count);
        Assert.Equal(20, results[0]); // (10 + 0) * 2
        Assert.Equal(22, results[1]); // (10 + 1) * 2
        Assert.Equal(24, results[2]); // (10 + 2) * 2

        // Verify enhanced handler executed
        var executions = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executions);
        Assert.Contains(nameof(EnhancedPluginStreamHandler), executions);

        // Verify only enhanced handler processed the request
        Assert.Empty(CorePluginStreamHandler.ProcessedRequests);
        Assert.Single(EnhancedPluginStreamHandler.ProcessedRequests);
    }

    [Fact]
    public async Task Test_AppendStreamHandlers_EmptyConfiguration()
    {
        // Clean up static state
        CorePluginStreamHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with stream handler
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.StreamRequestHandlerTypes = [typeof(CorePluginStreamHandler)];
            options.RegisterStreamRequestHandlers = true;
        });

        // Step 2: Append with empty configuration
        serviceContainer.AppendSnowberryMediator(options =>
        {
            // Empty configuration
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that original functionality still works
        var request = new PluginStreamRequest
        {
            Source = "Original test",
            Count = 2,
            StartValue = 5
        };

        var results = new List<int>();
        await foreach (var item in mediator.CreateStreamAsync<PluginStreamRequest, int>(request))
        {
            results.Add(item);
        }

        // Verify original handler still works
        Assert.Equal(2, results.Count);
        Assert.Equal(5, results[0]);
        Assert.Equal(6, results[1]);

        var executions = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(executions);
        Assert.Contains(nameof(CorePluginStreamHandler), executions);

        Assert.Single(CorePluginStreamHandler.ProcessedRequests);
    }

    #endregion

    #region Mixed Handler Tests

    [Fact]
    public async Task Test_AppendMixedHandlers_AllTypes()
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        CorePluginRequestHandler.Clear();
        ComplexPluginRequestHandler.Clear();
        CorePluginStreamHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with core handlers
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RequestHandlerTypes = [typeof(CorePluginRequestHandler)];
            options.StreamRequestHandlerTypes = [typeof(CorePluginStreamHandler)];
            options.RegisterNotificationHandlers = true;
            options.RegisterRequestHandlers = true;
            options.RegisterStreamRequestHandlers = true;
        });

        // Step 2: Append additional handlers of all types
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RequestHandlerTypes = [typeof(ComplexPluginRequestHandler)];
            options.RegisterNotificationHandlers = true;
            options.RegisterRequestHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test notification handlers
        var notification = new PluginNotification
        {
            PluginName = "MixedTest",
            Message = "Mixed handlers test",
            Version = 1
        };

        await mediator.PublishAsync(notification);

        var notificationExecutions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, notificationExecutions.Count);
        Assert.Contains(nameof(CorePluginHandler), notificationExecutions);
        Assert.Contains(nameof(AdditionalPluginHandler), notificationExecutions);

        // Reset tracking ONLY for execution trackers, not the handler collections
        PipelineExecutionTracker.Clear();
        StreamPipelineExecutionTracker.Clear();
        NotificationHandlerExecutionTracker.Clear();

        // Step 4: Test request handlers
        var pluginRequest = new PluginRequest
        {
            RequestData = "Mixed test request",
            ProcessingLevel = 2
        };

        var complexRequest = new ComplexPluginRequest
        {
            Data = "Mixed complex request",
            ProcessingSteps = 4
        };

        var pluginResult = await mediator.SendAsync<PluginRequest, string>(pluginRequest);
        var complexResult = await mediator.SendAsync<ComplexPluginRequest, string>(complexRequest);

        var requestExecutions = PipelineExecutionTracker.GetExecutionOrder();
        Assert.Equal(2, requestExecutions.Count);
        Assert.Contains(nameof(CorePluginRequestHandler), requestExecutions);
        Assert.Contains(nameof(ComplexPluginRequestHandler), requestExecutions);

        // Reset tracking ONLY for execution trackers, not the handler collections
        PipelineExecutionTracker.Clear();
        StreamPipelineExecutionTracker.Clear();

        // Step 5: Test stream handler
        var streamRequest = new PluginStreamRequest
        {
            Source = "Mixed stream test",
            Count = 2,
            StartValue = 1
        };

        var streamResults = new List<int>();
        await foreach (var item in mediator.CreateStreamAsync<PluginStreamRequest, int>(streamRequest))
        {
            streamResults.Add(item);
        }

        var streamExecutions = StreamPipelineExecutionTracker.GetExecutionOrder();
        Assert.Single(streamExecutions);
        Assert.Contains(nameof(CorePluginStreamHandler), streamExecutions);

        // Verify all handlers processed their respective messages
        Assert.Single(CorePluginHandler.ProcessedNotifications);
        Assert.Single(AdditionalPluginHandler.ProcessedNotifications);
        Assert.Single(CorePluginRequestHandler.ProcessedRequests);
        Assert.Single(ComplexPluginRequestHandler.ProcessedRequests);
        Assert.Single(CorePluginStreamHandler.ProcessedRequests);

        // Verify correct results from request handlers
        Assert.Contains("Core processed", pluginResult);
        Assert.Contains("Mixed test request", pluginResult);
        Assert.Contains("Complex processed", complexResult);
        Assert.Contains("Mixed complex request", complexResult);
        Assert.Equal([1, 2], streamResults);
    }

    [Fact]
    public async Task Test_AppendHandlers_ConcurrentPluginLoading()
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        ThirdPartyPluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Simulate concurrent plugin loading
        var appendTasks = new List<Task>
        {
            Task.Run(() => serviceContainer.AppendSnowberryMediator(options =>
            {
                options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
                options.RegisterNotificationHandlers = true;
            })),
            Task.Run(() => serviceContainer.AppendSnowberryMediator(options =>
            {
                options.NotificationHandlerTypes = [typeof(ThirdPartyPluginHandler)];
                options.RegisterNotificationHandlers = true;
            }))
        };

        await Task.WhenAll(appendTasks);

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that all handlers work after concurrent loading
        var notification = new PluginNotification
        {
            PluginName = "ConcurrentTest",
            Message = "Concurrent loading test",
            Version = 1
        };

        await mediator.PublishAsync(notification);

        // Verify all handlers executed
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(3, executions.Count);
        Assert.Contains(nameof(CorePluginHandler), executions);
        Assert.Contains(nameof(AdditionalPluginHandler), executions);
        Assert.Contains(nameof(ThirdPartyPluginHandler), executions);

        // Verify all handlers processed the notification
        Assert.Single(CorePluginHandler.ProcessedNotifications);
        Assert.Single(AdditionalPluginHandler.ProcessedNotifications);
        Assert.Single(ThirdPartyPluginHandler.ProcessedNotifications);
    }

    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task Test_AppendHandlers_DifferentServiceLifetimes(ServiceLifetime lifetime)
    {
        // Clean up static state
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true;
        }, serviceLifetime: lifetime);

        // Step 2: Append with same lifetime
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RegisterNotificationHandlers = true;
        }, serviceLifetime: lifetime);

        // Step 3: Test multiple scopes if applicable
        if (lifetime == ServiceLifetime.Scoped)
        {
            for (int i = 0; i < 3; i++)
            {
                using var scope = serviceContainer.CreateScope();
                var mediator = scope.ServiceFactory.GetService<IMediator>();

                var notification = new PluginNotification
                {
                    PluginName = $"ScopeTest{i}",
                    Message = $"Scope test {i}",
                    Version = i
                };

                await mediator.PublishAsync(notification);
            }

            // Verify all executions across scopes
            var executions = NotificationHandlerExecutionTracker.GetExecutions();
            Assert.Equal(6, executions.Count); // 2 handlers × 3 scopes

            Assert.Equal(3, executions.Count(e => e == nameof(CorePluginHandler)));
            Assert.Equal(3, executions.Count(e => e == nameof(AdditionalPluginHandler)));
        }
        else
        {
            var mediator = serviceContainer.GetService<IMediator>();

            var notification = new PluginNotification
            {
                PluginName = "LifetimeTest",
                Message = "Lifetime test",
                Version = 1
            };

            await mediator.PublishAsync(notification);

            var executions = NotificationHandlerExecutionTracker.GetExecutions();
            Assert.Equal(2, executions.Count);
            Assert.Contains(nameof(CorePluginHandler), executions);
            Assert.Contains(nameof(AdditionalPluginHandler), executions);
        }

        // Verify handlers processed notifications
        Assert.True(CorePluginHandler.ProcessedNotifications.Count > 0);
        Assert.True(AdditionalPluginHandler.ProcessedNotifications.Count > 0);
    }

    [Fact]
    public async Task Test_AppendHandlers_PreserveExistingRegistrations()
    {
        // Clean up static state
        SimpleNotificationHandler.ClearReceivedNotifications();
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator with existing system handlers
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.Assemblies = [typeof(SimpleNotification).Assembly];
            options.NotificationHandlerTypes = [
                typeof(SimpleNotificationHandler),
                typeof(CorePluginHandler)
            ];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Append additional plugin handlers
        serviceContainer.AppendSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(AdditionalPluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that existing handlers still work
        var simpleNotification = new SimpleNotification
        {
            Message = "Existing handler test",
            Value = 100
        };

        await mediator.PublishAsync(simpleNotification);

        // Verify existing handler still works
        Assert.Single(SimpleNotificationHandler.ReceivedNotifications);
        var receivedNotification = SimpleNotificationHandler.ReceivedNotifications.First();
        Assert.Equal(simpleNotification.Message, receivedNotification.Message);
        Assert.Equal(simpleNotification.Value, receivedNotification.Value);

        // Reset for plugin test
        CorePluginHandler.Clear();
        AdditionalPluginHandler.Clear();
        NotificationHandlerExecutionTracker.Clear();

        // Step 4: Test that appended handlers work
        var pluginNotification = new PluginNotification
        {
            PluginName = "PreserveTest",
            Message = "Preservation test",
            Version = 1
        };

        await mediator.PublishAsync(pluginNotification);

        // Verify both plugin handlers execute
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Equal(2, executions.Count);
        Assert.Contains(nameof(CorePluginHandler), executions);
        Assert.Contains(nameof(AdditionalPluginHandler), executions);
    }

    [Fact]
    public async Task Test_AppendHandlers_EmptyConfiguration()
    {
        // Clean up static state
        CorePluginHandler.Clear();

        using var serviceContainer = new ServiceContainer();

        // Step 1: Register initial mediator
        serviceContainer.AddSnowberryMediator(options =>
        {
            options.NotificationHandlerTypes = [typeof(CorePluginHandler)];
            options.RegisterNotificationHandlers = true;
        });

        // Step 2: Append with empty configuration (should not break anything)
        serviceContainer.AppendSnowberryMediator(options =>
        {
            // Empty configuration
        });

        var mediator = serviceContainer.GetService<IMediator>();

        // Step 3: Test that original functionality still works
        var notification = new PluginNotification
        {
            PluginName = "EmptyTest",
            Message = "Empty append test",
            Version = 1
        };

        await mediator.PublishAsync(notification);

        // Verify original handler still works
        var executions = NotificationHandlerExecutionTracker.GetExecutions();
        Assert.Single(executions);
        Assert.Contains(nameof(CorePluginHandler), executions);

        Assert.Single(CorePluginHandler.ProcessedNotifications);
    }

    #endregion
}
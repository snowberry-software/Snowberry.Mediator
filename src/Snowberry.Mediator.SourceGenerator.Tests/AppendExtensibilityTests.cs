using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>
/// Verifies that the generated append entry point extends an already-configured mediator (the plugin / modular
/// registration scenario) by merging into the existing global registries rather than discarding a fresh one.
/// </summary>
public class AppendExtensibilityTests
{
    private const string c_NotificationAppendSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;
        using Snowberry.Mediator.Extensions.DependencyInjection;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public static class Sink { public static readonly System.Collections.Generic.List<string> Items = new(); }

        public sealed class ExtNote : INotification { }

        public sealed class HostNoteHandler : INotificationHandler<ExtNote>
        {
            public ValueTask HandleAsync(ExtNote notification, CancellationToken cancellationToken = default)
            {
                Sink.Items.Add("host");
                return default;
            }
        }

        public sealed class PluginNoteHandler : INotificationHandler<ExtNote>
        {
            public ValueTask HandleAsync(ExtNote notification, CancellationToken cancellationToken = default)
            {
                Sink.Items.Add("plugin");
                return default;
            }
        }

        public static class Runner
        {
            public static string Run()
            {
                var services = new ServiceCollection();

                // Host configures the mediator with only its own handler (reflection-based registration).
                services.AddSnowberryMediator(options =>
                {
                    options.NotificationHandlerTypes = [typeof(HostNoteHandler)];
                    options.RegisterNotificationHandlers = true;
                });

                // A module extends the existing system through the generated append entry point.
                services.AddSnowberryMediator(append: true);

                var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
                mediator.PublishAsync(new ExtNote()).AsTask().GetAwaiter().GetResult();
                return string.Join(",", Sink.Items);
            }
        }
        """;

    private const string c_PipelineAppendSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;
        using Snowberry.Mediator.Abstractions.Pipeline;
        using Snowberry.Mediator.Extensions.DependencyInjection;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public static class Sink { public static readonly System.Collections.Generic.List<string> Items = new(); }

        public sealed class ExtReq : IRequest<ExtReq, string> { }

        public sealed class ExtReqHandler : IRequestHandler<ExtReq, string>
        {
            public ValueTask<string> HandleAsync(ExtReq request, CancellationToken cancellationToken = default) => new("core");
        }

        public sealed class HostBehavior : IPipelineBehavior<ExtReq, string>
        {
            public async ValueTask<string> HandleAsync<TNext>(ExtReq request, TNext next, CancellationToken cancellationToken = default)
                where TNext : struct, IPipelineContinuation<ExtReq, string>
            {
                Sink.Items.Add("host");
                return await next.InvokeAsync(request, cancellationToken);
            }
        }

        public sealed class PluginBehavior : IPipelineBehavior<ExtReq, string>
        {
            public async ValueTask<string> HandleAsync<TNext>(ExtReq request, TNext next, CancellationToken cancellationToken = default)
                where TNext : struct, IPipelineContinuation<ExtReq, string>
            {
                Sink.Items.Add("plugin");
                return await next.InvokeAsync(request, cancellationToken);
            }
        }

        public static class Runner
        {
            public static string Run()
            {
                var services = new ServiceCollection();

                // Host configures the mediator with its handler and one behavior (reflection-based registration).
                services.AddSnowberryMediator(options =>
                {
                    options.RequestHandlerTypes = [typeof(ExtReqHandler)];
                    options.PipelineBehaviorTypes = [typeof(HostBehavior)];
                    options.RegisterRequestHandlers = true;
                });

                // A module extends the existing pipeline through the generated append entry point.
                services.AddSnowberryMediator(append: true);

                var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();
                string result = mediator.SendAsync<ExtReq, string>(new ExtReq()).AsTask().GetAwaiter().GetResult();
                return result + ":" + string.Join(",", Sink.Items);
            }
        }
        """;

    [Fact]
    public void Test_Append_MergesNotificationHandlersIntoExistingRegistry()
    {
        var result = GeneratorTestHelper.Run(c_NotificationAppendSource);

        Assert.DoesNotContain(result.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("E2E.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        // Host handler must still fire, and the appended plugin handler must have merged into the host registry.
        Assert.Contains("host", value);
        Assert.Contains("plugin", value);
    }

    [Fact]
    public void Test_Append_MergesPipelineBehaviorsIntoExistingRegistry()
    {
        var result = GeneratorTestHelper.Run(c_PipelineAppendSource);

        Assert.DoesNotContain(result.OutputCompilation.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)assembly.GetType("E2E.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        // Core handler ran, and both the host and the appended plugin behavior executed in the same pipeline.
        Assert.Contains("core", value);
        Assert.Contains("host", value);
        Assert.Contains("plugin", value);
    }
}

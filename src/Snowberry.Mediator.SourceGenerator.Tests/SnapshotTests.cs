using VerifyXunit;

namespace Snowberry.Mediator.SourceGenerator.Tests;

public class SnapshotTests
{
    private const string Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Attributes;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;
        using Snowberry.Mediator.Abstractions.Pipeline;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace App;

        public sealed class GetUser : IRequest<GetUser, string> { }
        public sealed class GetUserHandler : IRequestHandler<GetUser, string>
        {
            public ValueTask<string> HandleAsync(GetUser request, CancellationToken cancellationToken = default) => new("u");
        }

        public sealed class UserCreated : INotification { }
        public sealed class UserCreatedHandler : INotificationHandler<UserCreated>
        {
            public ValueTask HandleAsync(UserCreated notification, CancellationToken cancellationToken = default) => default;
        }

        [PipelineOverwritePriority(Priority = 50)]
        public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : class, IRequest<TRequest, TResponse>
        {
            public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
                where TNext : struct, IPipelineContinuation<TRequest, TResponse>
                => next.InvokeAsync(request, cancellationToken);
        }
        """;

    [Fact]
    public Task Test_GeneratedSources_MatchSnapshot()
    {
        var result = GeneratorTestHelper.Run(Source);
        return Verifier.Verify(result.Driver).UseDirectory("Snapshots");
    }
}

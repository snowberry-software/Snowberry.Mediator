namespace Snowberry.Mediator.SourceGenerator.Tests;

public class DiagnosticsTests
{
    private const string c_Header = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace D;

        """;

    [Fact]
    public void Test_DuplicateRequestHandler_ReportsSBMED001()
    {
        const string body = """
            public sealed class DupRequest : IRequest<DupRequest, int> { }
            public sealed class Handler1 : IRequestHandler<DupRequest, int>
            {
                public ValueTask<int> HandleAsync(DupRequest request, CancellationToken cancellationToken = default) => new(1);
            }
            public sealed class Handler2 : IRequestHandler<DupRequest, int>
            {
                public ValueTask<int> HandleAsync(DupRequest request, CancellationToken cancellationToken = default) => new(2);
            }
            """;

        var result = GeneratorTestHelper.Run(c_Header + body);

        Assert.Contains("SBMED001", result.ReportedIds);
    }

    [Fact]
    public void Test_DuplicateStreamHandler_ReportsSBMED002()
    {
        const string body = """
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            public sealed class S : IStreamRequest<S, int> { }
            public sealed class SH1 : IStreamRequestHandler<S, int>
            {
                public async IAsyncEnumerable<int> HandleAsync(S request, [EnumeratorCancellation] CancellationToken cancellationToken = default) { yield return 1; await Task.CompletedTask; }
            }
            public sealed class SH2 : IStreamRequestHandler<S, int>
            {
                public async IAsyncEnumerable<int> HandleAsync(S request, [EnumeratorCancellation] CancellationToken cancellationToken = default) { yield return 2; await Task.CompletedTask; }
            }
            """;

        var result = GeneratorTestHelper.Run(c_Header + body);

        Assert.Contains("SBMED002", result.ReportedIds);
    }

    [Fact]
    public void Test_AbstractHandler_ReportsSBMED103()
    {
        const string body = """
            public sealed class AbsRequest : IRequest<AbsRequest, int> { }
            public abstract class AbstractHandler : IRequestHandler<AbsRequest, int>
            {
                public abstract ValueTask<int> HandleAsync(AbsRequest request, CancellationToken cancellationToken = default);
            }
            """;

        var result = GeneratorTestHelper.Run(c_Header + body);

        Assert.Contains("SBMED103", result.ReportedIds);
        Assert.Null(result.RegistrationSource);
    }

    [Fact]
    public void Test_NoHandlers_ReportsSBMED202()
    {
        const string source = """
            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace D;

            public class JustAClass { }
            """;

        var result = GeneratorTestHelper.Run(source);

        Assert.Contains("SBMED202", result.ReportedIds);
        Assert.Null(result.RegistrationSource);
    }

    [Fact]
    public void Test_MultipleTriggerAttributes_ReportsSBMED201()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            [assembly: Snowberry.Mediator.SnowberryMediator]
            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace D;

            public sealed class R : IRequest<R, int> { }
            public sealed class H : IRequestHandler<R, int>
            {
                public ValueTask<int> HandleAsync(R request, CancellationToken cancellationToken = default) => new(1);
            }
            """;

        var result = GeneratorTestHelper.Run(source);

        Assert.Contains("SBMED201", result.ReportedIds);
    }

    [Fact]
    public void Test_ValidProject_ReportsNoDiagnostics()
    {
        const string body = """
            public sealed class OkRequest : IRequest<OkRequest, int> { }
            public sealed class OkHandler : IRequestHandler<OkRequest, int>
            {
                public ValueTask<int> HandleAsync(OkRequest request, CancellationToken cancellationToken = default) => new(1);
            }
            """;

        var result = GeneratorTestHelper.Run(c_Header + body);

        Assert.Empty(result.ReportedIds);
    }
}

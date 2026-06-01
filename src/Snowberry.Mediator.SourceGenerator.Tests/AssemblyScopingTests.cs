namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>
/// Verifies the <c>ScanReferencedAssemblies</c> toggle and the <c>[assembly: SnowberryMediatorAssembly]</c>
/// include markers that scope which referenced assemblies the generator discovers handlers in.
/// </summary>
public class AssemblyScopingTests
{
    private const string c_LibSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        namespace Lib;

        public sealed class LibRequest : IRequest<LibRequest, int> { }

        public sealed class LibHandler : IRequestHandler<LibRequest, int>
        {
            public ValueTask<int> HandleAsync(LibRequest request, CancellationToken cancellationToken = default) => new(1);
        }
        """;

    private const string c_Lib2Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        namespace Lib2;

        public sealed class Lib2Request : IRequest<Lib2Request, int> { }

        public sealed class Lib2Handler : IRequestHandler<Lib2Request, int>
        {
            public ValueTask<int> HandleAsync(Lib2Request request, CancellationToken cancellationToken = default) => new(2);
        }
        """;

    // A root-assembly handler so tests can assert the current assembly is always scanned.
    private const string c_RootHandler = """
        public sealed class AppRequest : Snowberry.Mediator.Abstractions.Messages.IRequest<AppRequest, int> { }
        public sealed class AppHandler : Snowberry.Mediator.Abstractions.Handler.IRequestHandler<AppRequest, int>
        {
            public System.Threading.Tasks.ValueTask<int> HandleAsync(AppRequest request, System.Threading.CancellationToken cancellationToken = default) => new(0);
        }
        """;

    private static string BuildSource(string assemblyAttributes, string scanArg = "") => $$"""
        using Snowberry.Mediator.Abstractions;

        [assembly: Snowberry.Mediator.SnowberryMediator({{scanArg}})]
        {{assemblyAttributes}}

        namespace App;

        {{c_RootHandler}}
        """;

    [Fact]
    public void Test_ScanReferencedAssembliesFalse_NoMarkers_ScansOnlyRootAssembly()
    {
        var libReference = GeneratorTestHelper.CompileToReference(c_LibSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(
            BuildSource(assemblyAttributes: "", scanArg: "ScanReferencedAssemblies = false"), "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        // Root assembly is always scanned.
        Assert.Contains("typeof(global::App.AppHandler)", result.RegistrationSource);
        // The referenced assembly is excluded.
        Assert.DoesNotContain("LibHandler", result.RegistrationSource);
    }

    [Fact]
    public void Test_ScanReferencedAssembliesFalse_WithMarker_IncludesOnlyNamedAssembly()
    {
        var libReference = GeneratorTestHelper.CompileToReference(c_LibSource, "Lib");
        var lib2Reference = GeneratorTestHelper.CompileToReference(c_Lib2Source, "Lib2");
        var compilation = GeneratorTestHelper.CreateCompilation(
            BuildSource(
                assemblyAttributes: "[assembly: Snowberry.Mediator.SnowberryMediatorAssembly(typeof(Lib.LibRequest))]",
                scanArg: "ScanReferencedAssemblies = false"),
            "App",
            new[] { libReference, lib2Reference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        // The named assembly is included.
        Assert.Contains("typeof(global::Lib.LibHandler)", result.RegistrationSource);
        // The unnamed referenced assembly is excluded.
        Assert.DoesNotContain("Lib2Handler", result.RegistrationSource);
    }

    [Fact]
    public void Test_ScanReferencedAssembliesTrue_WithMarker_MarkerIgnored()
    {
        // With auto-scan on, the include marker is a no-op: every eligible assembly is already scanned.
        var libReference = GeneratorTestHelper.CompileToReference(c_LibSource, "Lib");
        var withMarker = GeneratorTestHelper.Run(GeneratorTestHelper.CreateCompilation(
            BuildSource(assemblyAttributes: "[assembly: Snowberry.Mediator.SnowberryMediatorAssembly(typeof(Lib.LibRequest))]"),
            "App", new[] { libReference }));
        var withoutMarker = GeneratorTestHelper.Run(GeneratorTestHelper.CreateCompilation(
            BuildSource(assemblyAttributes: ""), "App", new[] { libReference }));

        Scenarios.AssertNoErrors(withMarker);
        Scenarios.AssertNoErrors(withoutMarker);
        Assert.Contains("typeof(global::Lib.LibHandler)", withMarker.RegistrationSource);
        Assert.Equal(withoutMarker.RegistrationSource, withMarker.RegistrationSource);
    }

    [Fact]
    public void Test_DefaultUnset_ScansReferencedAssemblies()
    {
        // Backward-compat guard: no scoping configured behaves exactly as before.
        var libReference = GeneratorTestHelper.CompileToReference(c_LibSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(
            BuildSource(assemblyAttributes: ""), "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        Assert.Contains("typeof(global::Lib.LibHandler)", result.RegistrationSource);
        Assert.Contains("typeof(global::App.AppHandler)", result.RegistrationSource);
    }

    [Fact]
    public void Test_ScanReferencedAssembliesFalse_OpenGenericClosureRespectsScoping()
    {
        // An open-generic behavior in the root assembly must not close over a request from an unscanned assembly.
        const string mainWithBehavior = """
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Messages;
            using Snowberry.Mediator.Abstractions.Pipeline;

            [assembly: Snowberry.Mediator.SnowberryMediator(ScanReferencedAssemblies = false)]

            namespace App;

            public sealed class AppRequest : IRequest<AppRequest, int> { }
            public sealed class AppHandler : Snowberry.Mediator.Abstractions.Handler.IRequestHandler<AppRequest, int>
            {
                public ValueTask<int> HandleAsync(AppRequest request, CancellationToken cancellationToken = default) => new(0);
            }

            public sealed class LogBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : class, IRequest<TRequest, TResponse>
            {
                public ValueTask<TResponse> HandleAsync<TNext>(TRequest request, TNext next, CancellationToken cancellationToken = default)
                    where TNext : struct, IPipelineContinuation<TRequest, TResponse>
                    => next.InvokeAsync(request, cancellationToken);
            }
            """;

        var libReference = GeneratorTestHelper.CompileToReference(c_LibSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(mainWithBehavior, "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        // Closed over the root-assembly request only.
        Assert.Contains("global::App.LogBehavior<global::App.AppRequest, int>", result.RegistrationSource);
        // Never closed over the unscanned assembly's request.
        Assert.DoesNotContain("LibRequest", result.RegistrationSource);
    }
}

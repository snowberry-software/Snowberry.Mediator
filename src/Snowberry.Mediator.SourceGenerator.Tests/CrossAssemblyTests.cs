using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

public class CrossAssemblyTests
{
    private const string PublicLibSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        namespace Lib;

        public sealed class LibRequest : IRequest<LibRequest, int> { public int Value { get; set; } }

        public sealed class LibHandler : IRequestHandler<LibRequest, int>
        {
            public ValueTask<int> HandleAsync(LibRequest request, CancellationToken cancellationToken = default) => new(request.Value + 100);
        }
        """;

    private const string MainUsingLib = """
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace App;

        public static class Runner
        {
            public static string Run()
            {
                var mediator = new ServiceCollection().AddSnowberryMediator().BuildServiceProvider().GetRequiredService<IMediator>();
                return mediator.SendAsync<Lib.LibRequest, int>(new Lib.LibRequest { Value = 5 }).AsTask().GetAwaiter().GetResult().ToString();
            }
        }
        """;

    [Fact]
    public void Test_PublicHandlerInReferencedAssembly_IsDiscovered()
    {
        var libReference = GeneratorTestHelper.CompileToReference(PublicLibSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(MainUsingLib, "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        Assert.Contains("typeof(global::Lib.LibHandler)", result.RegistrationSource);
        Assert.Contains("typeof(global::Snowberry.Mediator.Abstractions.Handler.IRequestHandler<global::Lib.LibRequest, int>)", result.RegistrationSource);
    }

    [Fact]
    public void Test_CrossAssembly_DispatchesAtRuntime()
    {
        var (libReference, libImage) = GeneratorTestHelper.CompileAssembly(PublicLibSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(MainUsingLib, "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);
        Scenarios.AssertNoErrors(result);

        // Load the referenced (in-memory) assembly and resolve it by name when the app binds to it.
        var libAssembly = Assembly.Load(libImage);
        AssemblyLoadContext.Default.Resolving += (_, name) => name.Name == "Lib" ? libAssembly : null;
        var app = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var value = (string)app.GetType("App.Runner")!.GetMethod("Run")!.Invoke(null, null)!;

        Assert.Equal("105", value);
    }

    [Fact]
    public void Test_InternalHandlerWithInternalsVisibleTo_IsDiscovered()
    {
        const string libSource = """
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("App")]

            namespace Lib;

            internal sealed class SecretRequest : IRequest<SecretRequest, int> { }

            internal sealed class SecretHandler : IRequestHandler<SecretRequest, int>
            {
                public ValueTask<int> HandleAsync(SecretRequest request, CancellationToken cancellationToken = default) => new(1);
            }
            """;

        const string main = """
            using Snowberry.Mediator.Abstractions;

            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace App;

            public static class Marker { }
            """;

        var libReference = GeneratorTestHelper.CompileToReference(libSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(main, "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        Assert.Contains("typeof(global::Lib.SecretHandler)", result.RegistrationSource);
    }

    [Fact]
    public void Test_InternalHandlerWithoutInternalsVisibleTo_IsSkippedWithDiagnostic()
    {
        const string libSource = """
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            namespace Lib;

            internal sealed class SecretRequest : IRequest<SecretRequest, int> { }

            internal sealed class SecretHandler : IRequestHandler<SecretRequest, int>
            {
                public ValueTask<int> HandleAsync(SecretRequest request, CancellationToken cancellationToken = default) => new(1);
            }
            """;

        const string main = """
            using Snowberry.Mediator.Abstractions;

            [assembly: Snowberry.Mediator.SnowberryMediator]

            namespace App;

            public sealed class AppRequest : Snowberry.Mediator.Abstractions.Messages.IRequest<AppRequest, int> { }
            public sealed class AppHandler : Snowberry.Mediator.Abstractions.Handler.IRequestHandler<AppRequest, int>
            {
                public System.Threading.Tasks.ValueTask<int> HandleAsync(AppRequest request, System.Threading.CancellationToken cancellationToken = default)
                    => new(1);
            }
            """;

        var libReference = GeneratorTestHelper.CompileToReference(libSource, "Lib");
        var compilation = GeneratorTestHelper.CreateCompilation(main, "App", new[] { libReference });

        var result = GeneratorTestHelper.Run(compilation);

        Scenarios.AssertNoErrors(result);
        Assert.DoesNotContain("SecretHandler", result.RegistrationSource);
        Assert.Contains("SBMED101", result.ReportedIds);
    }
}

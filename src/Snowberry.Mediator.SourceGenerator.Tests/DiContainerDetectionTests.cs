namespace Snowberry.Mediator.SourceGenerator.Tests;

public class DiContainerDetectionTests
{
    private const string TriggerWithHandlerSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public sealed class PingRequest : IRequest<PingRequest, int> { }

        public sealed class PingHandler : IRequestHandler<PingRequest, int>
        {
            public ValueTask<int> HandleAsync(PingRequest request, CancellationToken cancellationToken = default) => new(1);
        }
        """;

    [Fact]
    public void Test_MicrosoftEntryPoint_Skipped_WhenIntegrationPackageAbsent()
    {
        // IServiceCollection stays referenced, but the integration assembly that defines MicrosoftServiceContext
        // is not. Detection probes the bridge context type, so the Microsoft entry point must be skipped while
        // the Snowberry one (still referenced) remains.
        var compilation = GeneratorTestHelper.CreateCompilationWithout(
            TriggerWithHandlerSource,
            "Snowberry.Mediator.Extensions.DependencyInjection.dll");

        var source = GeneratorTestHelper.Run(compilation).RegistrationSource;

        Assert.NotNull(source);
        Assert.Contains("SnowberryMediatorRegistration", source);
        Assert.DoesNotContain("SnowberryMediatorServiceCollectionExtensions", source);
        Assert.DoesNotContain("MicrosoftServiceContext", source);
        Assert.Contains("SnowberryMediatorServiceRegistryExtensions", source);
    }

    [Fact]
    public void Test_NoEntryPoints_WhenNoIntegrationPackageReferenced()
    {
        // Neither integration assembly is referenced: only the container-agnostic registration core is emitted.
        var compilation = GeneratorTestHelper.CreateCompilationWithout(
            TriggerWithHandlerSource,
            "Snowberry.Mediator.Extensions.DependencyInjection.dll",
            "Snowberry.Mediator.DependencyInjection.dll");

        var source = GeneratorTestHelper.Run(compilation).RegistrationSource;

        Assert.NotNull(source);
        Assert.Contains("SnowberryMediatorRegistration", source);
        Assert.DoesNotContain("SnowberryMediatorServiceCollectionExtensions", source);
        Assert.DoesNotContain("SnowberryMediatorServiceRegistryExtensions", source);
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Snowberry.Mediator.SourceGenerator.Tests;

public class CacheabilityTests
{
    private const string c_Source = """
        using System.Threading;
        using System.Threading.Tasks;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace App;

        public sealed class PingRequest : IRequest<PingRequest, int> { }
        public sealed class PingHandler : IRequestHandler<PingRequest, int>
        {
            public ValueTask<int> HandleAsync(PingRequest request, CancellationToken cancellationToken = default) => new(1);
        }
        """;

    private static GeneratorDriver CreateDriver()
        => CSharpGeneratorDriver.Create(
            generators: new[] { new SnowberryMediatorGenerator().AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    [Fact]
    public void Test_UnrelatedEdit_DiscoveryOutputUnchanged()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(c_Source, "App");
        var driver = CreateDriver().RunGenerators(compilation);

        // Unrelated edit: add a non-handler type in another tree.
        var edited = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Unrelated { internal sealed class Foo { public int X; } }"));
        driver = driver.RunGenerators(edited);

        var steps = driver.GetRunResult().Results[0].TrackedSteps[TrackingNames.c_Discovery];
        Assert.All(steps, step => Assert.All(step.Outputs, output =>
            Assert.True(
                output.Reason is IncrementalStepRunReason.Unchanged or IncrementalStepRunReason.Cached,
                $"Expected Unchanged/Cached after an unrelated edit but got {output.Reason}.")));
    }

    [Fact]
    public void Test_AddingHandler_DiscoveryOutputChanges()
    {
        var compilation = GeneratorTestHelper.CreateCompilation(c_Source, "App");
        var driver = CreateDriver().RunGenerators(compilation);

        var edited = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("""
            using System.Threading;
            using System.Threading.Tasks;
            using Snowberry.Mediator.Abstractions.Handler;
            using Snowberry.Mediator.Abstractions.Messages;

            namespace Extra;

            public sealed class ExtraRequest : IRequest<ExtraRequest, int> { }
            public sealed class ExtraHandler : IRequestHandler<ExtraRequest, int>
            {
                public ValueTask<int> HandleAsync(ExtraRequest request, CancellationToken cancellationToken = default) => new(2);
            }
            """));
        driver = driver.RunGenerators(edited);

        var steps = driver.GetRunResult().Results[0].TrackedSteps[TrackingNames.c_Discovery];
        Assert.Contains(steps, step => step.Outputs.Any(o =>
            o.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New));
    }
}

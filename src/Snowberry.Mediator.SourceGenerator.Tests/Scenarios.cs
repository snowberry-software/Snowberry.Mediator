using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>Helpers for building and executing end-to-end generator scenarios.</summary>
internal static class Scenarios
{
    /// <summary>Common usings, the opt-in attribute, a file-scoped <c>E2E</c> namespace, and a result sink.</summary>
    public const string Prelude = """
        using System.Collections.Generic;
        using System.Runtime.CompilerServices;
        using System.Threading;
        using System.Threading.Tasks;
        using Microsoft.Extensions.DependencyInjection;
        using Snowberry.Mediator.Abstractions;
        using Snowberry.Mediator.Abstractions.Attributes;
        using Snowberry.Mediator.Abstractions.Handler;
        using Snowberry.Mediator.Abstractions.Messages;
        using Snowberry.Mediator.Abstractions.Pipeline;

        [assembly: Snowberry.Mediator.SnowberryMediator]

        namespace E2E;

        public static class Sink { public static readonly List<string> Items = new(); }

        """;

    public static void AssertNoErrors(GeneratorResult result)
    {
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        var compileErrors = result.OutputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        Assert.True(compileErrors.Length == 0, "Generated code did not compile:\n" + string.Join("\n", compileErrors));
    }

    /// <summary>Builds <c>Prelude + body</c>, runs the generator, emits, loads, and invokes <c>E2E.Runner.Run()</c>.</summary>
    public static string Run(string body)
    {
        var result = GeneratorTestHelper.Run(Prelude + body);
        AssertNoErrors(result);

        var assembly = GeneratorTestHelper.EmitAndLoad(result.OutputCompilation);
        var runner = assembly.GetType("E2E.Runner")
            ?? throw new InvalidOperationException("E2E.Runner not found in generated assembly.");

        return (string)runner.GetMethod("Run")!.Invoke(null, null)!;
    }
}

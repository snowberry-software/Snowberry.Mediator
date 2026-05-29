using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Snowberry.Mediator.SourceGenerator.Tests;

/// <summary>Result of running the generator over a compilation.</summary>
internal sealed record GeneratorResult(
    GeneratorDriver Driver,
    CSharpCompilation OutputCompilation,
    ImmutableArray<Diagnostic> Diagnostics,
    GeneratorDriverRunResult RunResult)
{
    /// <summary>The generated registration source (excludes the post-init attribute), or null if none.</summary>
    public string? RegistrationSource
    {
        get
        {
            foreach (var tree in RunResult.GeneratedTrees)
            {
                if (tree.FilePath.Contains("Registration"))
                    return tree.ToString();
            }

            return null;
        }
    }

    public IEnumerable<string> ReportedIds => Diagnostics.Select(d => d.Id);
}

internal static class GeneratorTestHelper
{
    private static readonly MetadataReference[] s_References = CreateReferences();

    private static MetadataReference[] CreateReferences()
    {
        var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        void Add(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                references[Path.GetFileName(path)] = MetadataReference.CreateFromFile(path);
        }

        // Reference only the shared-framework assemblies (TRUSTED_PLATFORM_ASSEMBLIES also contains the test
        // bin, which would pollute discovery with Tests.Common handlers); add the mediator assemblies explicitly.
        var coreDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (var path in tpa.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Path.GetDirectoryName(path), coreDirectory, StringComparison.OrdinalIgnoreCase))
            {
                Add(path);
            }
        }

        // Snowberry + DI assemblies live in the test bin, not the shared framework.
        Add(typeof(global::Snowberry.Mediator.Abstractions.IMediator).Assembly.Location);
        Add(typeof(global::Snowberry.Mediator.Mediator).Assembly.Location);
        Add(typeof(global::Snowberry.Mediator.DependencyInjection.Shared.Contracts.IServiceContext).Assembly.Location);
        Add(typeof(global::Snowberry.Mediator.Extensions.DependencyInjection.MicrosoftServiceContext).Assembly.Location);
        Add(typeof(global::Snowberry.Mediator.DependencyInjection.SnowberryServiceContext).Assembly.Location);
        Add(typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
        Add(typeof(global::Microsoft.Extensions.DependencyInjection.ServiceCollection).Assembly.Location);
        Add(typeof(global::Microsoft.Extensions.DependencyInjection.ServiceProvider).Assembly.Location);
        Add(typeof(global::Snowberry.DependencyInjection.Abstractions.Interfaces.IServiceRegistry).Assembly.Location);

        return references.Values.ToArray();
    }

    public static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName = "Tests",
        IEnumerable<MetadataReference>? extraReferences = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var references = extraReferences is null ? s_References : s_References.Concat(extraReferences);

        return CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>
    /// Creates a compilation with the standard reference set minus any assembly whose file name appears in
    /// <paramref name="excludedAssemblyFileNames"/>. Simulates a consumer that has not referenced a particular
    /// Snowberry integration package.
    /// </summary>
    /// <param name="source">The C# source to compile.</param>
    /// <param name="excludedAssemblyFileNames">The assembly file names to omit (for example,
    /// <c>Snowberry.Mediator.Extensions.DependencyInjection.dll</c>).</param>
    /// <returns>The created compilation without the excluded references.</returns>
    public static CSharpCompilation CreateCompilationWithout(string source, params string[] excludedAssemblyFileNames)
    {
        var excluded = new HashSet<string>(excludedAssemblyFileNames, StringComparer.OrdinalIgnoreCase);
        var references = s_References.Where(r =>
            r is not PortableExecutableReference pe
            || pe.FilePath is not { } path
            || !excluded.Contains(Path.GetFileName(path)));

        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        return CSharpCompilation.Create(
            "Tests",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    public static GeneratorResult Run(CSharpCompilation compilation)
    {
        var generator = new SnowberryMediatorGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return new GeneratorResult(driver, (CSharpCompilation)output, diagnostics, driver.GetRunResult());
    }

    public static GeneratorResult Run(string source, string assemblyName = "Tests", IEnumerable<MetadataReference>? extraReferences = null)
        => Run(CreateCompilation(source, assemblyName, extraReferences));

    /// <summary>Emits the (already generator-updated) compilation and loads it; throws on any compile error.</summary>
    public static Assembly EmitAndLoad(CSharpCompilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
            throw new InvalidOperationException("Emit failed:\n" + string.Join("\n", errors));
        }

        return Assembly.Load(stream.ToArray());
    }

    /// <summary>Builds a referenced ("handlers") assembly image to test cross-assembly discovery/execution.</summary>
    public static (MetadataReference Reference, byte[] Image) CompileAssembly(string source, string assemblyName)
    {
        var compilation = CreateCompilation(source, assemblyName);
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
            throw new InvalidOperationException($"Reference assembly '{assemblyName}' failed to compile:\n" + string.Join("\n", errors));
        }

        var image = stream.ToArray();
        return (MetadataReference.CreateFromImage(image, filePath: assemblyName + ".dll"), image);
    }

    public static MetadataReference CompileToReference(string source, string assemblyName)
        => CompileAssembly(source, assemblyName).Reference;
}

using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Extensions.DependencyInjection;

namespace Snowberry.Mediator.Benchmarks;

/// <summary>
/// Startup/registration cost: reflection assembly-scanning vs the source-generated, reflection-free path.
/// The dispatch hot path is unaffected by the generator (see <see cref="MediatorBenchmarks"/>); these
/// benchmarks measure only the one-time registration cost the generator eliminates.
/// </summary>
[MemoryDiagnoser]
public class RegistrationBenchmarks
{
    private static readonly Assembly s_assembly = typeof(NoPipelineRequest).Assembly;

    /// <summary>Builds a provider by scanning the benchmark assembly with reflection.</summary>
    /// <returns>The resolved mediator.</returns>
    [Benchmark(Baseline = true)]
    public IMediator Startup_ReflectionScan()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(options =>
        {
            options.Assemblies = new List<Assembly> { s_assembly };
            options.ScanPipelineBehaviors = true;
            options.ScanStreamPipelineBehaviors = true;
            options.ScanNotificationHandlers = true;
        }, ServiceLifetime.Singleton);

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    /// <summary>Builds a provider via the source-generated, reflection-free registration.</summary>
    /// <returns>The resolved mediator.</returns>
    [Benchmark]
    public IMediator Startup_Generated()
    {
        var services = new ServiceCollection();
        services.AddSnowberryMediator(ServiceLifetime.Singleton);

        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.OpenTelemetry;

namespace Snowberry.Mediator.Extensions.OpenTelemetry;

/// <summary>
/// Provides extension methods on <see cref="IServiceCollection"/> for adding Snowberry.Mediator
/// OpenTelemetry instrumentation to a Microsoft.Extensions.DependencyInjection container.
/// </summary>
public static class OpenTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MediatorTelemetryOptions"/> and <see cref="MediatorInstrumentation"/>
    /// as singletons and decorates the existing <see cref="IMediator"/> registration with
    /// <see cref="InstrumentedMediator"/>. Must be called after the Snowberry.Mediator
    /// registration extension has registered <see cref="IMediator"/>.
    /// </summary>
    /// <param name="services">The service collection to add the registrations to.</param>
    /// <param name="configure">An optional callback used to configure the <see cref="MediatorTelemetryOptions"/> instance.</param>
    /// <returns>The supplied <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// No <see cref="IMediator"/> registration was found in <paramref name="services"/>.
    /// </exception>
    [UnconditionalSuppressMessage(
        "Trimming", "IL2072",
        Justification = "IMediator implementation was registered by AddSnowberryMediator, which preserves its constructor; re-resolution does not require additional members.")]
    public static IServiceCollection AddSnowberryMediatorOpenTelemetry(
        this IServiceCollection services,
        Action<MediatorTelemetryOptions>? configure = null)
    {
        _ = services ?? throw new ArgumentNullException(nameof(services));

        // Idempotency guard: presence of MediatorInstrumentation means AddSnowberryMediatorOpenTelemetry
        // has already run. Return before building options, so a repeated call neither re-runs the configure
        // callback (which could flip the process-wide per-step span flags) nor wraps the decorator again.
        for (int i = 0; i < services.Count; i++)
        {
            if (services[i].ServiceType == typeof(MediatorInstrumentation))
                return services;
        }

        // Locate the IMediator registration to decorate before touching the collection, so the
        // "called too early" failure leaves no registrations behind and a later retry still decorates.
        int index = -1;
        for (int i = 0; i < services.Count; i++)
        {
            var candidate = services[i];
            if (candidate.ServiceType == typeof(IMediator)
                && (candidate.ImplementationInstance is IMediator || candidate.ImplementationFactory is not null || candidate.ImplementationType is not null))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            throw new InvalidOperationException(
                "AddSnowberryMediatorOpenTelemetry must be called after AddSnowberryMediator. " +
                "No IMediator registration was found in the service collection.");

        var options = global::Snowberry.Mediator.OpenTelemetry.Internals.TelemetryRegistration.BuildOptions(configure);

        services.TryAddSingleton(options);
        services.TryAddSingleton<MediatorInstrumentation>();

        var d = services[index];
        Func<IServiceProvider, IMediator> innerFactory;
        if (d.ImplementationInstance is IMediator instance)
        {
            innerFactory = _ => instance;
        }
        else if (d.ImplementationFactory is { } factory)
        {
            innerFactory = sp => (IMediator)factory(sp);
        }
        else
        {
            // Promote the concrete IMediator type to its own descriptor so it can be resolved from
            // the decorator factory. Same lifetime as the original.
            var implType = d.ImplementationType!;
            services.TryAdd(new ServiceDescriptor(implType, implType, d.Lifetime));
            innerFactory = sp => (IMediator)sp.GetRequiredService(implType);
        }

        services[index] = new ServiceDescriptor(
            typeof(IMediator),
            sp => new InstrumentedMediator(
                innerFactory(sp),
                sp.GetRequiredService<MediatorInstrumentation>(),
                sp.GetRequiredService<MediatorTelemetryOptions>()),
            d.Lifetime);

        return services;
    }
}
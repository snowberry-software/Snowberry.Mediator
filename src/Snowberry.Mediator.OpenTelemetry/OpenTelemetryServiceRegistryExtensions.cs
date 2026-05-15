using System.Diagnostics.CodeAnalysis;
using Snowberry.DependencyInjection.Abstractions;
using Snowberry.DependencyInjection.Abstractions.Extensions;
using Snowberry.DependencyInjection.Abstractions.Implementation;
using Snowberry.DependencyInjection.Abstractions.Interfaces;
using Snowberry.Mediator.Abstractions;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Provides extension methods on <see cref="IServiceRegistry"/> for adding Snowberry.Mediator
/// OpenTelemetry instrumentation to a Snowberry.DependencyInjection container.
/// </summary>
public static class OpenTelemetryServiceRegistryExtensions
{
    /// <summary>
    /// Registers <see cref="MediatorTelemetryOptions"/> and <see cref="MediatorInstrumentation"/>
    /// as singletons and registers the <see cref="IMediator"/> service with a factory that yields
    /// an <see cref="InstrumentedMediator"/> wrapping a <see cref="Mediator"/>. Must be called
    /// before the Snowberry.Mediator registration extension has registered <see cref="IMediator"/>.
    /// Calling this method a second time on the same <paramref name="registry"/> is a no-op.
    /// </summary>
    /// <param name="registry">The service registry to add the registrations to.</param>
    /// <param name="configure">An optional callback used to configure the <see cref="MediatorTelemetryOptions"/> instance.</param>
    /// <param name="lifetime">The <see cref="ServiceLifetime"/> applied to the <see cref="IMediator"/> registration. Should match the lifetime passed to the Snowberry.Mediator registration extension. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The supplied <paramref name="registry"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// An <see cref="IMediator"/> registration is already present in <paramref name="registry"/>.
    /// </exception>
    /// <remarks>
    /// The decorator's inner mediator is always constructed as a <see cref="Mediator"/>. Custom
    /// <see cref="IMediator"/> implementations registered against the same registry are not
    /// invoked through this decorator.
    /// </remarks>
    [UnconditionalSuppressMessage(
        "Trimming", "IL2072",
        Justification = "Mediator type is resolved through IServiceProvider; the constructor is preserved by the explicit type reference.")]
    public static IServiceRegistry AddSnowberryMediatorOpenTelemetry(
        this IServiceRegistry registry,
        Action<MediatorTelemetryOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        _ = registry ?? throw new ArgumentNullException(nameof(registry));

        if (registry.IsServiceRegistered<MediatorInstrumentation>(serviceKey: null))
        {
            // Idempotency: re-applying the options would silently overwrite the singleton's state.
            // Skip — the original registration wins.
            return registry;
        }

        if (registry.IsServiceRegistered<IMediator>(serviceKey: null))
            throw new InvalidOperationException(
                "AddSnowberryMediatorOpenTelemetry must be called before AddSnowberryMediator. " +
                "Snowberry.DependencyInjection's registry does not permit re-registration of IMediator after it has been registered.");

        var options = new MediatorTelemetryOptions();
        configure?.Invoke(options);

        if (options.EnablePipelineBehaviorSpans)
            MediatorDiagnostics.EnablePipelineSpans();
        if (options.EnableNotificationHandlerSpans)
            MediatorDiagnostics.EnableNotificationSpans();

        registry.RegisterSingleton(options);
        var instrumentation = new MediatorInstrumentation(options);
        registry.RegisterSingleton(instrumentation);

        var descriptor = new ServiceDescriptor(typeof(IMediator), typeof(InstrumentedMediator), lifetime)
        {
            InstanceFactory = (sp, _) => new InstrumentedMediator(new Mediator(sp), instrumentation, options),
        };
        registry.Register(descriptor, serviceKey: null);

        return registry;
    }
}

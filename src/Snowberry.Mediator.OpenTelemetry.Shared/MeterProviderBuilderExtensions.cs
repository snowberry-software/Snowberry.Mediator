using OpenTelemetry.Metrics;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Provides extension methods on <see cref="MeterProviderBuilder"/> for subscribing to the
/// Snowberry.Mediator <see cref="System.Diagnostics.Metrics.Meter"/>.
/// </summary>
public static class MeterProviderBuilderExtensions
{
    /// <summary>
    /// Adds the default Snowberry.Mediator meter — <c>"Snowberry.Mediator"</c> — to
    /// <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The meter provider builder to register the meter with.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static MeterProviderBuilder AddSnowberryMediatorInstrumentation(this MeterProviderBuilder builder)
        => AddSnowberryMediatorInstrumentation(builder, MediatorTelemetryConventions.c_DefaultSourceName);

    /// <summary>
    /// Adds the Snowberry.Mediator meter to <paramref name="builder"/>, using
    /// <paramref name="meterName"/> as the name of the meter to subscribe to.
    /// </summary>
    /// <param name="builder">The meter provider builder to register the meter with.</param>
    /// <param name="meterName">The name of the <see cref="System.Diagnostics.Metrics.Meter"/> to subscribe to.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="meterName"/> is <see langword="null"/>.
    /// </exception>
    public static MeterProviderBuilder AddSnowberryMediatorInstrumentation(this MeterProviderBuilder builder, string meterName)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));
        _ = meterName ?? throw new ArgumentNullException(nameof(meterName));

        return builder.AddMeter(meterName);
    }
}
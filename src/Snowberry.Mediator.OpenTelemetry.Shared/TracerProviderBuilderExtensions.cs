using OpenTelemetry.Trace;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Provides extension methods on <see cref="TracerProviderBuilder"/> for subscribing to the
/// Snowberry.Mediator <see cref="System.Diagnostics.ActivitySource"/> instances.
/// </summary>
public static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds the default Snowberry.Mediator activity sources — <c>"Snowberry.Mediator"</c>,
    /// <c>"Snowberry.Mediator.Pipeline"</c>, and <c>"Snowberry.Mediator.Notification"</c> — to
    /// <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The tracer provider builder to register the sources with.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddSnowberryMediatorInstrumentation(this TracerProviderBuilder builder)
        => AddSnowberryMediatorInstrumentation(builder, "Snowberry.Mediator");

    /// <summary>
    /// Adds the Snowberry.Mediator activity sources to <paramref name="builder"/>, using
    /// <paramref name="sourceName"/> as the name of the top-level dispatch source. The per-step
    /// sources (<c>"Snowberry.Mediator.Pipeline"</c> and <c>"Snowberry.Mediator.Notification"</c>)
    /// are always registered with their fixed names.
    /// </summary>
    /// <param name="builder">The tracer provider builder to register the sources with.</param>
    /// <param name="sourceName">The name of the top-level dispatch <see cref="System.Diagnostics.ActivitySource"/> to subscribe to.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> or <paramref name="sourceName"/> is <see langword="null"/>.
    /// </exception>
    public static TracerProviderBuilder AddSnowberryMediatorInstrumentation(this TracerProviderBuilder builder, string sourceName)
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));
        _ = sourceName ?? throw new ArgumentNullException(nameof(sourceName));

        builder.AddSource(sourceName);
        builder.AddSource("Snowberry.Mediator.Pipeline");
        builder.AddSource("Snowberry.Mediator.Notification");
        return builder;
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Snowberry.Mediator.OpenTelemetry;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Adds the common ".NET Aspire" service defaults (OpenTelemetry, health checks, service discovery and
/// resilient HTTP clients) to a host, and wires the Snowberry.Mediator OpenTelemetry instrumentation
/// into the tracing and metrics pipelines.
/// </summary>
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Adds OpenTelemetry, default health checks, service discovery and resilient HTTP client defaults to
    /// <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default.
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default.
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics and tracing for <paramref name="builder"/>, including the
    /// Snowberry.Mediator dispatch, pipeline-behavior and notification-handler instrumentation, and registers
    /// the OTLP exporter when an endpoint is configured.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddSnowberryMediatorInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSnowberryMediatorInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        bool useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
            builder.Services.AddOpenTelemetry().UseOtlpExporter();

        return builder;
    }

    /// <summary>
    /// Adds a default liveness health check named <c>self</c> to <paramref name="builder"/>.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The supplied <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure the app is responsive.
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps the default health-check endpoints (<c>/health</c> and <c>/alive</c>) when running in the
    /// development environment. Has no effect for hosts without an HTTP server, such as workers.
    /// </summary>
    /// <param name="app">The web application to map the endpoints on.</param>
    /// <returns>The supplied <paramref name="app"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        _ = app ?? throw new ArgumentNullException(nameof(app));

        // Adding health checks endpoints to applications in non-development environments has security
        // implications. See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these
        // endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for the app to be considered ready to accept traffic after starting.
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for the app to be considered alive.
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live"),
            });
        }

        return app;
    }
}

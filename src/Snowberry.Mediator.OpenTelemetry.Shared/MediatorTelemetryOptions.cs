using System.Diagnostics;
using System.Diagnostics.Metrics;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Options that configure the OpenTelemetry instrumentation applied to an
/// <see cref="IMediator"/> by the <see cref="InstrumentedMediator"/> decorator.
/// </summary>
public sealed class MediatorTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether counter and histogram measurements are recorded
    /// for each dispatch. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the registration extension should call
    /// <see cref="MediatorDiagnostics.EnableNotificationSpans"/>, causing an <see cref="Activity"/>
    /// to be created for every notification handler invocation. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableNotificationHandlerSpans { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the registration extension should call
    /// <see cref="MediatorDiagnostics.EnablePipelineSpans"/>, causing an <see cref="Activity"/>
    /// to be created for every pipeline behavior invocation. Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnablePipelineBehaviorSpans { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Activity"/> objects are created for
    /// each dispatch. Defaults to <see langword="true"/>.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional callback invoked when a dispatched request, stream, or publish
    /// throws. The callback receives the active <see cref="Activity"/>, the request or
    /// notification object, and the thrown <see cref="Exception"/>.
    /// </summary>
    public Action<Activity, object, Exception>? EnrichWithException { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked once an <see cref="Activity"/> has been created
    /// for an <see cref="INotification"/> publish, before the notification is delivered. The
    /// callback receives the activity and the notification object.
    /// </summary>
    public Action<Activity, object>? EnrichWithNotification { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked once an <see cref="Activity"/> has been created
    /// for an <see cref="IRequest{TRequest, TResponse}"/> or <see cref="IStreamRequest{TRequest, TResponse}"/>
    /// dispatch, before the inner mediator is invoked. The callback receives the activity and
    /// the request object.
    /// </summary>
    public Action<Activity, object>? EnrichWithRequest { get; set; }

    /// <summary>
    /// Gets or sets an optional callback invoked when an <see cref="IRequest{TRequest, TResponse}"/>
    /// dispatch completes successfully. The callback receives the active <see cref="Activity"/>,
    /// the request object, and the response value returned by the handler.
    /// </summary>
    public Action<Activity, object, object>? EnrichWithResponse { get; set; }

    /// <summary>
    /// Gets or sets an optional filter applied before instrumentation is performed for a
    /// dispatch. Returning <see langword="false"/> bypasses <see cref="Activity"/> creation,
    /// metric recording, and the enrichment callbacks for that dispatch; the request is still
    /// forwarded to the inner mediator. Defaults to <see langword="null"/>, which applies no
    /// filter.
    /// </summary>
    public Func<object, bool>? Filter { get; set; }

    /// <summary>
    /// Gets or sets the name used for both the <see cref="ActivitySource"/> and the
    /// <see cref="Meter"/> created by <see cref="MediatorInstrumentation"/>. Defaults to
    /// <c>"Snowberry.Mediator"</c>.
    /// </summary>
    public string SourceName { get; set; } = "Snowberry.Mediator";
}
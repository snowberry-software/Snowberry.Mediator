using System.Diagnostics;
using System.Diagnostics.Metrics;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Messages;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// Owns the <see cref="System.Diagnostics.ActivitySource"/>, <see cref="System.Diagnostics.Metrics.Meter"/>
/// and per-operation <see cref="Counter{T}"/> and <see cref="Histogram{T}"/> instruments emitted by
/// the <see cref="InstrumentedMediator"/> decorator.
/// </summary>
public sealed class MediatorInstrumentation : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediatorInstrumentation"/> class. The value of
    /// <see cref="MediatorTelemetryOptions.SourceName"/> is used as the name of both
    /// <see cref="ActivitySource"/> and <see cref="Meter"/>.
    /// </summary>
    /// <param name="options">The options whose <see cref="MediatorTelemetryOptions.SourceName"/> determines the source name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public MediatorInstrumentation(MediatorTelemetryOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));

        ActivitySource = new ActivitySource(options.SourceName);
        Meter = new Meter(options.SourceName);

        SendCount = Meter.CreateCounter<long>("snowberry.mediator.send.count");
        SendDuration = Meter.CreateHistogram<double>("snowberry.mediator.send.duration", unit: "ms");

        StreamCount = Meter.CreateCounter<long>("snowberry.mediator.stream.count");
        StreamDuration = Meter.CreateHistogram<double>("snowberry.mediator.stream.duration", unit: "ms");

        PublishCount = Meter.CreateCounter<long>("snowberry.mediator.publish.count");
        PublishDuration = Meter.CreateHistogram<double>("snowberry.mediator.publish.duration", unit: "ms");
    }

    /// <summary>
    /// Disposes the underlying <see cref="ActivitySource"/> and <see cref="Meter"/>.
    /// </summary>
    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }

    /// <summary>
    /// Gets the <see cref="ActivitySource"/> from which dispatch-level <see cref="Activity"/>
    /// instances are emitted.
    /// </summary>
    public ActivitySource ActivitySource { get; }

    /// <summary>
    /// Gets the <see cref="Meter"/> from which the dispatch counters and duration histograms are
    /// emitted.
    /// </summary>
    public Meter Meter { get; }

    /// <summary>
    /// Gets the <see cref="Counter{T}"/> incremented for every
    /// <see cref="IMediatorPublisher.PublishAsync{TNotification}"/> dispatch. Instrument name:
    /// <c>snowberry.mediator.publish.count</c>.
    /// </summary>
    public Counter<long> PublishCount { get; }

    /// <summary>
    /// Gets the <see cref="Histogram{T}"/> that records the duration of every
    /// <see cref="IMediatorPublisher.PublishAsync{TNotification}"/> dispatch in milliseconds.
    /// Instrument name: <c>snowberry.mediator.publish.duration</c>.
    /// </summary>
    public Histogram<double> PublishDuration { get; }

    /// <summary>
    /// Gets the <see cref="Counter{T}"/> incremented for every <see cref="IMediatorSender.SendAsync{TRequest, TResponse}"/>
    /// dispatch. Instrument name: <c>snowberry.mediator.send.count</c>.
    /// </summary>
    public Counter<long> SendCount { get; }

    /// <summary>
    /// Gets the <see cref="Histogram{T}"/> that records the duration of every
    /// <see cref="IMediatorSender.SendAsync{TRequest, TResponse}"/> dispatch in milliseconds.
    /// Instrument name: <c>snowberry.mediator.send.duration</c>.
    /// </summary>
    public Histogram<double> SendDuration { get; }

    /// <summary>
    /// Gets the <see cref="Counter{T}"/> incremented for every
    /// <see cref="IMediatorSender.CreateStreamAsync{TRequest, TResponse}"/> dispatch. Instrument
    /// name: <c>snowberry.mediator.stream.count</c>.
    /// </summary>
    public Counter<long> StreamCount { get; }

    /// <summary>
    /// Gets the <see cref="Histogram{T}"/> that records the total enumeration duration of every
    /// <see cref="IMediatorSender.CreateStreamAsync{TRequest, TResponse}"/> dispatch in
    /// milliseconds. Instrument name: <c>snowberry.mediator.stream.duration</c>.
    /// </summary>
    public Histogram<double> StreamDuration { get; }
}
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Snowberry.Mediator.Tests.OpenTelemetry;

/// <summary>
/// Captures stopped <see cref="Activity"/> instances and metric measurements for the
/// Snowberry.Mediator activity sources / meter. Test fixtures own one of these and dispose it on
/// teardown to prevent cross-test pollution via the process-wide <see cref="ActivitySource"/>
/// listener registry.
/// </summary>
internal sealed class OpenTelemetryListenerCapture : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly object _lock = new();
    private readonly List<MetricMeasurement> _measurements = [];
    private readonly MeterListener _meterListener;
    private readonly List<Activity> _stopped = [];

    public OpenTelemetryListenerCapture(string sourceName)
    {
        // Force W3C IDs so TraceId is populated on .NET Framework targets. Setting these is
        // process-wide; the OpenTelemetry test collection is serialized so cross-test races
        // do not apply.
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        _activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName
                                  || s.Name == "Snowberry.Mediator.Pipeline"
                                  || s.Name == "Snowberry.Mediator.Notification",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                lock (_lock) _stopped.Add(a);
            },
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == sourceName)
                    listener.EnableMeasurementEvents(instrument);
            },
        };
        _meterListener.SetMeasurementEventCallback<long>(OnLongMeasurement);
        _meterListener.SetMeasurementEventCallback<double>(OnDoubleMeasurement);
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    private void OnDoubleMeasurement(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        => RecordMeasurement(instrument, value, tags);

    private void OnLongMeasurement(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        => RecordMeasurement(instrument, (double)value, tags);

    private void RecordMeasurement(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copied = new KeyValuePair<string, object?>[tags.Length];
        tags.CopyTo(copied);
        lock (_lock)
            _measurements.Add(new MetricMeasurement(instrument.Name, value, copied));
    }

    public IReadOnlyList<MetricMeasurement> Measurements
    {
        get { lock (_lock) return _measurements.ToArray(); }
    }

    public IReadOnlyList<Activity> StoppedActivities
    {
        get { lock (_lock) return _stopped.ToArray(); }
    }
}

internal readonly struct MetricMeasurement
{
    public MetricMeasurement(string instrumentName, double value, KeyValuePair<string, object?>[] tags)
    {
        InstrumentName = instrumentName;
        Value = value;
        Tags = tags;
    }

    public object? Tag(string key)
    {
        foreach (var t in Tags)
            if (t.Key == key) return t.Value;
        return null;
    }

    public string InstrumentName { get; }
    public KeyValuePair<string, object?>[] Tags { get; }
    public double Value { get; }
}
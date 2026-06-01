using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Snowberry.Mediator.Abstractions;
using Snowberry.Mediator.Abstractions.Mediator;
using Snowberry.Mediator.Abstractions.Messages;
using Snowberry.Mediator.OpenTelemetry.Internals;

namespace Snowberry.Mediator.OpenTelemetry;

/// <summary>
/// An <see cref="IMediator"/> decorator that emits <see cref="Activity"/> objects and metric
/// measurements for each <see cref="IMediatorSender.SendAsync{TRequest, TResponse}"/>,
/// <see cref="IMediatorSender.CreateStreamAsync{TRequest, TResponse}"/>, and
/// <see cref="IMediatorPublisher.PublishAsync{TNotification}"/> dispatch, forwarding the call to
/// an inner <see cref="IMediator"/> instance.
/// </summary>
public sealed class InstrumentedMediator : IMediator
{
    private readonly bool _enableMetrics;
    private readonly bool _enableTracing;
    private readonly IMediator _inner;
    private readonly MediatorInstrumentation _instrumentation;
    private readonly MediatorTelemetryOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedMediator"/> class that wraps the
    /// supplied inner <see cref="IMediator"/>.
    /// </summary>
    /// <param name="inner">The mediator that the decorator forwards each dispatch to.</param>
    /// <param name="instrumentation">The instrumentation providing the <see cref="ActivitySource"/> and metric instruments to use.</param>
    /// <param name="options">The options controlling tracing, metrics, enrichment, and filtering behavior.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="inner"/>, <paramref name="instrumentation"/>, or <paramref name="options"/>
    /// is <see langword="null"/>.
    /// </exception>
    public InstrumentedMediator(
        IMediator inner,
        MediatorInstrumentation instrumentation,
        MediatorTelemetryOptions options)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _enableTracing = options.EnableTracing;
        _enableMetrics = options.EnableMetrics;
    }

    private static double ElapsedMilliseconds(long startTimestamp)
        => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    private static void RecordHookFailure(Activity activity, Exception ex, string hookName)
    {
        var tags = new ActivityTagsCollection
        {
            { MediatorTelemetryConventions.Tags.c_ExceptionType, ex.GetType().FullName },
            { MediatorTelemetryConventions.Tags.c_ExceptionMessage, ex.Message },
            { MediatorTelemetryConventions.Tags.c_HookName, hookName },
        };
        activity.AddEvent(new ActivityEvent(MediatorTelemetryConventions.ActivityNames.c_EnrichmentFailedEvent, tags: tags));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<TResponse> CreateStreamAsync<TRequest, TResponse>(
        IStreamRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken = default)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
    {
        var inst = _instrumentation;
        bool tracingActive = _enableTracing && inst.ActivitySource.HasListeners();
        bool metricsActive = _enableMetrics && (inst.StreamCount.Enabled || inst.StreamDuration.Enabled);

        if (!tracingActive && !metricsActive)
            return _inner.CreateStreamAsync(request, cancellationToken);

        if (_options.Filter is { } filter && !filter(request))
            return _inner.CreateStreamAsync(request, cancellationToken);

        return CreateStreamInstrumentedAsync(request, tracingActive, metricsActive, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var inst = _instrumentation;
        bool tracingActive = _enableTracing && inst.ActivitySource.HasListeners();
        bool metricsActive = _enableMetrics && (inst.PublishCount.Enabled || inst.PublishDuration.Enabled);

        if (!tracingActive && !metricsActive)
            return _inner.PublishAsync(notification, cancellationToken);

        if (_options.Filter is { } filter && !filter(notification!))
            return _inner.PublishAsync(notification, cancellationToken);

        return PublishInstrumentedAsync(notification, tracingActive, metricsActive, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        IRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken = default)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        var inst = _instrumentation;
        bool tracingActive = _enableTracing && inst.ActivitySource.HasListeners();
        bool metricsActive = _enableMetrics && (inst.SendCount.Enabled || inst.SendDuration.Enabled);

        if (!tracingActive && !metricsActive)
            return _inner.SendAsync(request, cancellationToken);

        if (_options.Filter is { } filter && !filter(request))
            return _inner.SendAsync(request, cancellationToken);

        return SendInstrumentedAsync(request, tracingActive, metricsActive, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> CreateStreamInstrumentedAsync<TRequest, TResponse>(
        IStreamRequest<TRequest, TResponse> request,
        bool tracingActive,
        bool metricsActive,
        [EnumeratorCancellation] CancellationToken ct)
        where TRequest : class, IStreamRequest<TRequest, TResponse>
    {
        var inst = _instrumentation;
        string typeName = TypeNameCache<TRequest>.s_Name;

        Activity? activity = tracingActive
            ? inst.ActivitySource.StartActivity(MediatorTelemetryConventions.ActivityNames.c_StreamPrefix + typeName, ActivityKind.Internal)
            : null;
        if (activity is not null)
        {
            activity.SetTag(MediatorTelemetryConventions.Tags.c_RequestType, typeName);
            activity.SetTag(MediatorTelemetryConventions.Tags.c_ResponseType, TypeNameCache<TResponse>.s_Name);
            activity.SetTag(MediatorTelemetryConventions.Tags.c_Operation, MediatorTelemetryConventions.Operations.c_Stream);
            InvokeEnrichRequest(activity, request);
        }

        long start = metricsActive ? Stopwatch.GetTimestamp() : 0;
        string status = MediatorTelemetryConventions.Status.c_Failure;
        // 'yield return' cannot live inside a try/catch, so enumerate manually: the inner try/catch captures the
        // exception thrown by MoveNextAsync (handler throw or cancellation) while 'yield return' stays in the outer
        // try/finally. This lets the finally enrich with the exception detail - matching Send/Publish - while the
        // early-consumer-break case (no exception, finally runs via iterator dispose) keeps a bare Error status.
        Exception? error = null;
        await using var enumerator = _inner.CreateStreamAsync(request, ct).GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    error = ex;
                    throw;
                }

                if (!hasNext)
                    break;

                yield return enumerator.Current;
            }
            status = MediatorTelemetryConventions.Status.c_Success;
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        finally
        {
            if (status != MediatorTelemetryConventions.Status.c_Success && activity is not null)
            {
                if (error is not null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, error.Message);
                    InvokeEnrichException(activity, request, error);
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                }
            }
            if (metricsActive)
            {
                var tags = new TagList
                {
                    { MediatorTelemetryConventions.Tags.c_MetricType, typeName },
                    { MediatorTelemetryConventions.Tags.c_MetricStatus, status },
                };
                if (inst.StreamCount.Enabled) inst.StreamCount.Add(1, tags);
                if (inst.StreamDuration.Enabled) inst.StreamDuration.Record(ElapsedMilliseconds(start), tags);
            }
            activity?.Dispose();
        }
    }

    private void InvokeEnrichException(Activity activity, object request, Exception exception)
    {
        var hook = _options.EnrichWithException;
        if (hook is null) return;
        try { hook(activity, request, exception); }
        catch (Exception ex) { RecordHookFailure(activity, ex, MediatorTelemetryConventions.HookNames.c_EnrichWithException); }
    }

    private void InvokeEnrichNotification(Activity activity, object notification)
    {
        var hook = _options.EnrichWithNotification;
        if (hook is null) return;
        try { hook(activity, notification); }
        catch (Exception ex) { RecordHookFailure(activity, ex, MediatorTelemetryConventions.HookNames.c_EnrichWithNotification); }
    }

    private void InvokeEnrichRequest(Activity activity, object request)
    {
        var hook = _options.EnrichWithRequest;
        if (hook is null) return;
        try { hook(activity, request); }
        catch (Exception ex) { RecordHookFailure(activity, ex, MediatorTelemetryConventions.HookNames.c_EnrichWithRequest); }
    }

    private void InvokeEnrichResponse(Activity activity, object request, object response)
    {
        var hook = _options.EnrichWithResponse;
        if (hook is null) return;
        try { hook(activity, request, response); }
        catch (Exception ex) { RecordHookFailure(activity, ex, MediatorTelemetryConventions.HookNames.c_EnrichWithResponse); }
    }

    private async ValueTask PublishInstrumentedAsync<TNotification>(
        TNotification notification,
        bool tracingActive,
        bool metricsActive,
        CancellationToken ct)
        where TNotification : INotification
    {
        var inst = _instrumentation;
        string typeName = TypeNameCache<TNotification>.s_Name;

        Activity? activity = tracingActive
            ? inst.ActivitySource.StartActivity(MediatorTelemetryConventions.ActivityNames.c_PublishPrefix + typeName, ActivityKind.Internal)
            : null;
        if (activity is not null)
        {
            activity.SetTag(MediatorTelemetryConventions.Tags.c_NotificationType, typeName);
            activity.SetTag(MediatorTelemetryConventions.Tags.c_Operation, MediatorTelemetryConventions.Operations.c_Publish);
            InvokeEnrichNotification(activity, notification!);
        }

        long start = metricsActive ? Stopwatch.GetTimestamp() : 0;
        string status = MediatorTelemetryConventions.Status.c_Failure;
        try
        {
            await _inner.PublishAsync(notification, ct).ConfigureAwait(false);
            status = MediatorTelemetryConventions.Status.c_Success;
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                InvokeEnrichException(activity, notification!, ex);
            }
            throw;
        }
        finally
        {
            if (metricsActive)
            {
                var tags = new TagList
                {
                    { MediatorTelemetryConventions.Tags.c_MetricType, typeName },
                    { MediatorTelemetryConventions.Tags.c_MetricStatus, status },
                };
                if (inst.PublishCount.Enabled) inst.PublishCount.Add(1, tags);
                if (inst.PublishDuration.Enabled) inst.PublishDuration.Record(ElapsedMilliseconds(start), tags);
            }
            activity?.Dispose();
        }
    }

    private async ValueTask<TResponse> SendInstrumentedAsync<TRequest, TResponse>(
        IRequest<TRequest, TResponse> request,
        bool tracingActive,
        bool metricsActive,
        CancellationToken ct)
        where TRequest : class, IRequest<TRequest, TResponse>
    {
        var inst = _instrumentation;
        string typeName = TypeNameCache<TRequest>.s_Name;

        Activity? activity = tracingActive
            ? inst.ActivitySource.StartActivity(MediatorTelemetryConventions.ActivityNames.c_SendPrefix + typeName, ActivityKind.Internal)
            : null;
        if (activity is not null)
        {
            activity.SetTag(MediatorTelemetryConventions.Tags.c_RequestType, typeName);
            activity.SetTag(MediatorTelemetryConventions.Tags.c_ResponseType, TypeNameCache<TResponse>.s_Name);
            activity.SetTag(MediatorTelemetryConventions.Tags.c_Operation, MediatorTelemetryConventions.Operations.c_Send);
            InvokeEnrichRequest(activity, request);
        }

        long start = metricsActive ? Stopwatch.GetTimestamp() : 0;
        string status = MediatorTelemetryConventions.Status.c_Failure;
        try
        {
            var result = await _inner.SendAsync(request, ct).ConfigureAwait(false);
            status = MediatorTelemetryConventions.Status.c_Success;
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                InvokeEnrichResponse(activity, request, result!);
            }
            return result;
        }
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                InvokeEnrichException(activity, request, ex);
            }
            throw;
        }
        finally
        {
            if (metricsActive)
            {
                var tags = new TagList
                {
                    { MediatorTelemetryConventions.Tags.c_MetricType, typeName },
                    { MediatorTelemetryConventions.Tags.c_MetricStatus, status },
                };
                if (inst.SendCount.Enabled) inst.SendCount.Add(1, tags);
                if (inst.SendDuration.Enabled) inst.SendDuration.Record(ElapsedMilliseconds(start), tags);
            }
            activity?.Dispose();
        }
    }

    internal IMediator Inner => _inner;
}
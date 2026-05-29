using System;
using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>Configuration read from the <c>[assembly: SnowberryMediator]</c> attribute.</summary>
/// <param name="RegisterRequestHandlers">Whether request handlers are registered.</param>
/// <param name="RegisterStreamRequestHandlers">Whether stream request handlers are registered.</param>
/// <param name="RegisterNotificationHandlers">Whether notification handlers are registered.</param>
/// <param name="RegisterPipelineBehaviors">Whether pipeline behaviors are registered.</param>
/// <param name="RegisterStreamPipelineBehaviors">Whether stream pipeline behaviors are registered.</param>
internal readonly record struct MediatorConfig(
    bool RegisterRequestHandlers,
    bool RegisterStreamRequestHandlers,
    bool RegisterNotificationHandlers,
    bool RegisterPipelineBehaviors,
    bool RegisterStreamPipelineBehaviors)
{
    /// <summary>The default configuration, which registers every handler category.</summary>
    public static readonly MediatorConfig Default = new(true, true, true, true, true);
}

/// <summary>A concrete request or stream-request handler registration.</summary>
/// <param name="IsStream"><see langword="true"/> for a stream request handler; otherwise <see langword="false"/>.</param>
/// <param name="HandlerFqn">The fully-qualified handler type name.</param>
/// <param name="RequestFqn">The fully-qualified request type name.</param>
/// <param name="ResponseFqn">The fully-qualified response type name.</param>
internal readonly record struct RequestHandlerModel(
    bool IsStream,
    string HandlerFqn,
    string RequestFqn,
    string ResponseFqn) : IEquatable<RequestHandlerModel>;

/// <summary>A concrete notification handler registration (open-generic handlers are flattened into these).</summary>
/// <param name="HandlerFqn">The fully-qualified handler type name (closed, for a flattened open-generic handler).</param>
/// <param name="NotificationFqn">The fully-qualified notification type name.</param>
/// <param name="FromOpenGeneric"><see langword="true"/> if this entry was flattened from an open-generic handler.</param>
internal readonly record struct NotificationHandlerModel(
    string HandlerFqn,
    string NotificationFqn,
    bool FromOpenGeneric) : IEquatable<NotificationHandlerModel>;

/// <summary>One closed instantiation of an open-generic pipeline behavior.</summary>
/// <param name="ClosedHandlerFqn">The fully-qualified closed behavior type name.</param>
/// <param name="RequestFqn">The fully-qualified request type name the behavior is closed over.</param>
/// <param name="ResponseFqn">The fully-qualified response type name the behavior is closed over.</param>
internal readonly record struct ClosedBehaviorInstance(
    string ClosedHandlerFqn,
    string RequestFqn,
    string ResponseFqn) : IEquatable<ClosedBehaviorInstance>;

/// <summary>A pipeline or stream-pipeline behavior registration (concrete or open-generic).</summary>
/// <param name="IsStream"><see langword="true"/> for a stream pipeline behavior; otherwise <see langword="false"/>.</param>
/// <param name="IsOpenGeneric"><see langword="true"/> for an open-generic behavior; otherwise <see langword="false"/>.</param>
/// <param name="HandlerFqn">The fully-qualified concrete behavior type name (empty for open generics).</param>
/// <param name="OpenHandlerTypeOf">The unbound open-generic type-of expression (empty for concrete behaviors).</param>
/// <param name="RequestFqn">The fully-qualified request type name (empty for open generics).</param>
/// <param name="ResponseFqn">The fully-qualified response type name (empty for open generics).</param>
/// <param name="Priority">The behavior priority when <paramref name="HasPriority"/> is <see langword="true"/>.</param>
/// <param name="HasPriority"><see langword="true"/> if a <c>[PipelineOverwritePriority]</c> value was found.</param>
/// <param name="ClosedInstances">The closed instantiations to register (open generics only).</param>
internal readonly record struct BehaviorModel(
    bool IsStream,
    bool IsOpenGeneric,
    string HandlerFqn,
    string OpenHandlerTypeOf,
    string RequestFqn,
    string ResponseFqn,
    int Priority,
    bool HasPriority,
    EquatableArray<ClosedBehaviorInstance> ClosedInstances) : IEquatable<BehaviorModel>;

/// <summary>A diagnostic captured during discovery, carried through the cached model and reported at output.</summary>
/// <param name="Id">The diagnostic ID (for example, <c>SBMED001</c>).</param>
/// <param name="Args">The message-format arguments for the diagnostic.</param>
/// <param name="Location">The source location, or <see langword="null"/> for no location.</param>
internal readonly record struct DiagnosticInfo(
    string Id,
    EquatableArray<string> Args,
    LocationInfo? Location) : IEquatable<DiagnosticInfo>
{
    /// <summary>Builds the <see cref="Diagnostic"/> for this entry using the tracked descriptor and arguments.</summary>
    /// <returns>The reportable diagnostic.</returns>
    public Diagnostic ToDiagnostic()
    {
        var descriptor = Diagnostics.ById(Id);
        var location = Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None;

        if (Args.Count == 0)
            return Diagnostic.Create(descriptor, location);

        var args = new object[Args.Count];
        for (int i = 0; i < Args.Count; i++)
            args[i] = Args[i];

        return Diagnostic.Create(descriptor, location, args);
    }
}

/// <summary>An equatable, serializable source location (a raw <see cref="Location"/> would defeat caching).</summary>
/// <param name="FilePath">The source file path.</param>
/// <param name="Span">The character span within the file.</param>
/// <param name="LineSpan">The line/character span within the file.</param>
internal readonly record struct LocationInfo(
    string FilePath,
    TextSpanInfo Span,
    LinePositionSpanInfo LineSpan) : IEquatable<LocationInfo>
{
    /// <summary>Reconstructs a Roslyn <see cref="Location"/> from this value.</summary>
    /// <returns>The reconstructed location.</returns>
    public Location ToLocation() =>
        Location.Create(
            FilePath,
            new Microsoft.CodeAnalysis.Text.TextSpan(Span.Start, Span.Length),
            new Microsoft.CodeAnalysis.Text.LinePositionSpan(
                new Microsoft.CodeAnalysis.Text.LinePosition(LineSpan.StartLine, LineSpan.StartChar),
                new Microsoft.CodeAnalysis.Text.LinePosition(LineSpan.EndLine, LineSpan.EndChar)));

    /// <summary>Captures the first in-source location of <paramref name="symbol"/>.</summary>
    /// <param name="symbol">The symbol whose location is captured.</param>
    /// <returns>The location, or <see langword="null"/> if the symbol has no in-source location.</returns>
    public static LocationInfo? From(ISymbol symbol)
    {
        foreach (var loc in symbol.Locations)
        {
            if (loc.IsInSource)
                return From(loc);
        }

        return null;
    }

    /// <summary>Captures an in-source <paramref name="location"/> as an equatable value.</summary>
    /// <param name="location">The location to capture.</param>
    /// <returns>The captured location, or <see langword="null"/> if <paramref name="location"/> is not in source.</returns>
    public static LocationInfo? From(Location location)
    {
        if (!location.IsInSource)
            return null;

        var span = location.SourceSpan;
        var lineSpan = location.GetLineSpan();
        return new LocationInfo(
            location.SourceTree!.FilePath,
            new TextSpanInfo(span.Start, span.Length),
            new LinePositionSpanInfo(
                lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character,
                lineSpan.EndLinePosition.Line, lineSpan.EndLinePosition.Character));
    }
}

/// <summary>An equatable character span (start offset and length).</summary>
/// <param name="Start">The zero-based start offset.</param>
/// <param name="Length">The span length in characters.</param>
internal readonly record struct TextSpanInfo(int Start, int Length) : IEquatable<TextSpanInfo>;

/// <summary>An equatable line/character span.</summary>
/// <param name="StartLine">The zero-based start line.</param>
/// <param name="StartChar">The zero-based start character.</param>
/// <param name="EndLine">The zero-based end line.</param>
/// <param name="EndChar">The zero-based end character.</param>
internal readonly record struct LinePositionSpanInfo(int StartLine, int StartChar, int EndLine, int EndChar)
    : IEquatable<LinePositionSpanInfo>;

/// <summary>The full, value-equatable discovery result handed to the emitter.</summary>
/// <param name="RequestHandlers">The discovered request and stream-request handlers.</param>
/// <param name="Behaviors">The discovered pipeline and stream-pipeline behaviors.</param>
/// <param name="NotificationHandlers">The discovered notification handlers (open generics flattened).</param>
/// <param name="HasMicrosoftDI">Whether the <c>Microsoft.Extensions.DependencyInjection</c> entry point should be emitted.</param>
/// <param name="HasSnowberryDI">Whether the <c>Snowberry.DependencyInjection</c> entry point should be emitted.</param>
/// <param name="Diagnostics">The diagnostics gathered during discovery.</param>
internal readonly record struct DiscoveryModel(
    EquatableArray<RequestHandlerModel> RequestHandlers,
    EquatableArray<BehaviorModel> Behaviors,
    EquatableArray<NotificationHandlerModel> NotificationHandlers,
    bool HasMicrosoftDI,
    bool HasSnowberryDI,
    EquatableArray<DiagnosticInfo> Diagnostics) : IEquatable<DiscoveryModel>
{
    /// <summary>Gets a value indicating whether any handler, behavior or notification handler was discovered.</summary>
    public bool HasAnyHandlers =>
        RequestHandlers.Count > 0 || Behaviors.Count > 0 || NotificationHandlers.Count > 0;
}

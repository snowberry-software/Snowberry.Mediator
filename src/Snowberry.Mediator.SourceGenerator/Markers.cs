using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>Resolved marker symbols used to match handler/message types during discovery.</summary>
internal sealed class Markers
{
    /// <summary>The <c>IRequest&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IRequest { get; init; }

    /// <summary>The <c>IStreamRequest&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IStreamRequest { get; init; }

    /// <summary>The <c>INotification</c> marker symbol.</summary>
    public required INamedTypeSymbol INotification { get; init; }

    /// <summary>The <c>IRequestHandler&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IRequestHandler { get; init; }

    /// <summary>The <c>IStreamRequestHandler&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IStreamRequestHandler { get; init; }

    /// <summary>The <c>INotificationHandler&lt;&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol INotificationHandler { get; init; }

    /// <summary>The <c>IPipelineBehavior&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IPipelineBehavior { get; init; }

    /// <summary>The <c>IStreamPipelineBehavior&lt;,&gt;</c> marker symbol.</summary>
    public required INamedTypeSymbol IStreamPipelineBehavior { get; init; }

    /// <summary>The <c>PipelineOverwritePriorityAttribute</c> symbol.</summary>
    public required INamedTypeSymbol PriorityAttribute { get; init; }

    /// <summary>The generated <c>SnowberryMediatorAttribute</c> symbol, or <see langword="null"/> if not present.</summary>
    public INamedTypeSymbol? TriggerAttribute { get; init; }

    /// <summary>Resolves the marker symbols from the compilation.</summary>
    /// <param name="compilation">The compilation to resolve symbols from.</param>
    /// <returns>The resolved markers, or <see langword="null"/> if Snowberry.Mediator.Abstractions is not referenced.</returns>
    public static Markers? Resolve(Compilation compilation)
    {
        var requestHandler = compilation.GetTypeByMetadataName(WellKnown.c_IRequestHandler);
        if (requestHandler is null)
            return null;

        var request = compilation.GetTypeByMetadataName(WellKnown.c_IRequest);
        var streamRequest = compilation.GetTypeByMetadataName(WellKnown.c_IStreamRequest);
        var notification = compilation.GetTypeByMetadataName(WellKnown.c_INotification);
        var streamRequestHandler = compilation.GetTypeByMetadataName(WellKnown.c_IStreamRequestHandler);
        var notificationHandler = compilation.GetTypeByMetadataName(WellKnown.c_INotificationHandler);
        var pipelineBehavior = compilation.GetTypeByMetadataName(WellKnown.c_IPipelineBehavior);
        var streamPipelineBehavior = compilation.GetTypeByMetadataName(WellKnown.c_IStreamPipelineBehavior);
        var priorityAttribute = compilation.GetTypeByMetadataName(WellKnown.c_PipelineOverwritePriorityAttribute);

        if (request is null || streamRequest is null || notification is null || streamRequestHandler is null ||
            notificationHandler is null || pipelineBehavior is null || streamPipelineBehavior is null ||
            priorityAttribute is null)
        {
            return null;
        }

        return new Markers
        {
            IRequest = request,
            IStreamRequest = streamRequest,
            INotification = notification,
            IRequestHandler = requestHandler,
            IStreamRequestHandler = streamRequestHandler,
            INotificationHandler = notificationHandler,
            IPipelineBehavior = pipelineBehavior,
            IStreamPipelineBehavior = streamPipelineBehavior,
            PriorityAttribute = priorityAttribute,
            TriggerAttribute = compilation.GetTypeByMetadataName(WellKnown.c_TriggerAttributeMetadataName),
        };
    }
}

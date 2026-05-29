using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>Diagnostic descriptors emitted by the generator. IDs are tracked in AnalyzerReleases.Unshipped.md.</summary>
internal static class Diagnostics
{
    private const string Category = "SnowberryMediator";

    /// <summary>SBMED001: more than one handler is registered for the same request.</summary>
    public static readonly DiagnosticDescriptor DuplicateRequestHandler = new(
        "SBMED001",
        "Duplicate request handler",
        "Multiple handlers ('{0}' and '{1}') are registered for request '{2}'; a request must have exactly one handler",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>SBMED002: more than one handler is registered for the same stream request.</summary>
    public static readonly DiagnosticDescriptor DuplicateStreamRequestHandler = new(
        "SBMED002",
        "Duplicate stream request handler",
        "Multiple stream handlers ('{0}' and '{1}') are registered for stream request '{2}'; a stream request must have exactly one handler",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>SBMED101: a handler type is inaccessible to the consuming assembly and was skipped.</summary>
    public static readonly DiagnosticDescriptor InaccessibleHandler = new(
        "SBMED101",
        "Handler is inaccessible",
        "Type '{0}' implements a mediator handler interface but is not accessible from this assembly and was skipped; add [InternalsVisibleTo] on its assembly to register it",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>SBMED102: a handler's request/response/notification type argument is inaccessible.</summary>
    public static readonly DiagnosticDescriptor InaccessibleTypeArgument = new(
        "SBMED102",
        "Handler type argument is inaccessible",
        "Handler '{0}' is accessible but its type argument '{1}' is not, so it cannot be registered and was skipped",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>SBMED103: an abstract or static type implements a handler interface and was skipped.</summary>
    public static readonly DiagnosticDescriptor NonInstantiableHandler = new(
        "SBMED103",
        "Non-instantiable handler",
        "Type '{0}' implements a mediator handler interface but is abstract or static and was skipped",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>SBMED201: more than one trigger attribute was found; the first is used.</summary>
    public static readonly DiagnosticDescriptor MultipleTriggerAttributes = new(
        "SBMED201",
        "Multiple [SnowberryMediator] attributes",
        "Multiple [assembly: SnowberryMediator] attributes were found; the first one is used",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>SBMED202: the generator was triggered but discovered no handlers.</summary>
    public static readonly DiagnosticDescriptor NoHandlersDiscovered = new(
        "SBMED202",
        "No handlers discovered",
        "[assembly: SnowberryMediator] is present but no handlers were discovered; ensure handler projects are referenced and (for internal handlers) exposed via [InternalsVisibleTo]",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>Maps a diagnostic ID to its descriptor.</summary>
    /// <param name="id">The diagnostic ID.</param>
    /// <returns>The matching descriptor.</returns>
    public static DiagnosticDescriptor ById(string id) => id switch
    {
        "SBMED001" => DuplicateRequestHandler,
        "SBMED002" => DuplicateStreamRequestHandler,
        "SBMED101" => InaccessibleHandler,
        "SBMED102" => InaccessibleTypeArgument,
        "SBMED103" => NonInstantiableHandler,
        "SBMED201" => MultipleTriggerAttributes,
        "SBMED202" => NoHandlersDiscovered,
        _ => NoHandlersDiscovered,
    };
}

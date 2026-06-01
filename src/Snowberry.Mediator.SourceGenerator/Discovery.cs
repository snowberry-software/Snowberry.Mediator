using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>
/// Compile-time discovery of mediator handlers/behaviors/notification handlers across the current
/// compilation and referenced assemblies. Produces a value-equatable <see cref="DiscoveryModel"/>.
/// </summary>
internal static class Discovery
{
    /// <summary>Discovers all mediator handlers, behaviors and notification handlers for the compilation.</summary>
    /// <param name="compilation">The compilation to scan, including referenced assemblies.</param>
    /// <param name="ct">A token used to cancel discovery.</param>
    /// <returns>
    /// The discovery model, or <see langword="null"/> when the abstractions are not referenced or no
    /// <c>[assembly: SnowberryMediator]</c> attribute is present.
    /// </returns>
    public static DiscoveryModel? Discover(Compilation compilation, CancellationToken ct)
    {
        var markers = Markers.Resolve(compilation);
        if (markers is null)
            return null; // Snowberry.Mediator.Abstractions not referenced.

        var diagnostics = new List<DiagnosticInfo>();
        if (!TryReadConfig(compilation, markers, diagnostics, out var config))
            return null; // No [assembly: SnowberryMediator] -> the generator stays silent.

        bool hasMicrosoftDI = compilation.GetTypeByMetadataName(WellKnown.c_MicrosoftServiceContext) is not null;
        bool hasSnowberryDI = compilation.GetTypeByMetadataName(WellKnown.c_SnowberryServiceContext) is not null;

        var collector = new Collector(compilation, markers, config, diagnostics);

        // The current (root) assembly carries the opt-in attribute and is always scanned.
        foreach (var type in compilation.Assembly.GlobalNamespace.EnumerateAllTypes(ct))
            collector.Process(type);

        // null = scan every referenced assembly that references the abstractions (the default);
        // non-null = scan only the explicitly-included assemblies (ScanReferencedAssemblies = false).
        var includedAssemblies = config.ScanReferencedAssemblies
            ? null
            : CollectIncludedAssemblies(compilation, markers);

        foreach (var reference in compilation.References)
        {
            ct.ThrowIfCancellationRequested();

            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            if (!assembly.ReferencesAbstractions())
                continue;

            if (includedAssemblies is not null && !includedAssemblies.Contains(assembly))
                continue;

            foreach (var type in assembly.GlobalNamespace.EnumerateAllTypes(ct))
                collector.Process(type);
        }

        return collector.Build(hasMicrosoftDI, hasSnowberryDI);
    }

    private static bool TryReadConfig(Compilation compilation, Markers markers, List<DiagnosticInfo> diagnostics, out MediatorConfig config)
    {
        config = MediatorConfig.s_Default;

        AttributeData? triggerAttribute = null;
        int count = 0;
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, markers.TriggerAttribute))
                continue;

            count++;
            if (triggerAttribute is null)
            {
                triggerAttribute = attribute;
            }
            else
            {
                LocationInfo? location = null;
                if (attribute.ApplicationSyntaxReference is { } syntaxRef)
                    location = LocationInfo.From(syntaxRef.GetSyntax().GetLocation());

                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.s_MultipleTriggerAttributes.Id,
                    EquatableArray<string>.s_Empty,
                    location));
            }
        }

        if (count == 0)
            return false;

        config = ReadConfig(triggerAttribute!);
        return true;
    }

    private static MediatorConfig ReadConfig(AttributeData attribute)
    {
        bool requests = true, streamRequests = true, notifications = true, behaviors = true, streamBehaviors = true, scanReferenced = true;

        foreach (var named in attribute.NamedArguments)
        {
            if (named.Value.Value is not bool value)
                continue;

            switch (named.Key)
            {
                case "RegisterRequestHandlers": requests = value; break;
                case "RegisterStreamRequestHandlers": streamRequests = value; break;
                case "RegisterNotificationHandlers": notifications = value; break;
                case "RegisterPipelineBehaviors": behaviors = value; break;
                case "RegisterStreamPipelineBehaviors": streamBehaviors = value; break;
                case "ScanReferencedAssemblies": scanReferenced = value; break;
            }
        }

        return new MediatorConfig(requests, streamRequests, notifications, behaviors, streamBehaviors, scanReferenced);
    }

    /// <summary>
    /// Collects the assemblies named by <c>[assembly: SnowberryMediatorAssembly(typeof(T))]</c> markers, used as
    /// the include-set when <see cref="MediatorConfig.ScanReferencedAssemblies"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="compilation">The compilation whose assembly attributes are scanned for include markers.</param>
    /// <param name="markers">The resolved markers supplying the include-attribute symbol to match against.</param>
    /// <returns>
    /// The set of assemblies to include, compared with <see cref="SymbolEqualityComparer.Default"/>; empty when no
    /// marker is present.
    /// </returns>
    private static HashSet<IAssemblySymbol> CollectIncludedAssemblies(Compilation compilation, Markers markers)
    {
        var included = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        if (markers.AssemblyAttribute is null)
            return included;

        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, markers.AssemblyAttribute))
                continue;

            if (attribute.ConstructorArguments.Length != 1)
                continue;

            // typeof(T) ctor arg surfaces as a TypedConstant of kind Type whose Value is an ITypeSymbol.
            // ContainingAssembly is null for array/pointer types (e.g. typeof(int[])); skip those.
            if (attribute.ConstructorArguments[0].Value is ITypeSymbol { ContainingAssembly: { } markerAssembly })
                included.Add(markerAssembly);
        }

        return included;
    }
}

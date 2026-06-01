using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Snowberry.Mediator.SourceGenerator;

/// <summary>
/// Accumulates discovered handlers/behaviors/notification handlers while a set of types is walked, then
/// produces the final value-equatable <see cref="DiscoveryModel"/> (resolving open generics into closed
/// instantiations along the way).
/// </summary>
internal sealed class Collector
{
    private static readonly SymbolEqualityComparer s_Cmp = SymbolEqualityComparer.Default;

    private readonly Compilation _compilation;
    private readonly Markers _markers;
    private readonly MediatorConfig _config;
    private readonly List<DiagnosticInfo> _diagnostics;

    // Universe of message types (accessible), used to close open generics.
    private readonly Dictionary<string, (ITypeSymbol Req, ITypeSymbol Resp)> _requestPairs = new();
    private readonly Dictionary<string, (ITypeSymbol Req, ITypeSymbol Resp)> _streamPairs = new();
    private readonly Dictionary<string, ITypeSymbol> _notificationTypes = new();

    // Registrations.
    private readonly Dictionary<string, (string HandlerFqn, ISymbol Symbol)> _requestHandlers = new();
    private readonly Dictionary<string, (string HandlerFqn, ISymbol Symbol)> _streamHandlers = new();
    private readonly List<(string HandlerFqn, string Req, string Resp)> _requestHandlerModels = new();
    private readonly List<(string HandlerFqn, string Req, string Resp)> _streamHandlerModels = new();
    private readonly HashSet<string> _concreteNotificationKeys = new();
    private readonly List<(string HandlerFqn, string NotificationFqn)> _concreteNotificationHandlers = new();
    private readonly List<INamedTypeSymbol> _openNotificationHandlers = new();
    private readonly List<BehaviorModel> _concreteBehaviors = new();
    private readonly List<(INamedTypeSymbol Type, bool IsStream, int Priority, bool HasPriority)> _openBehaviors = new();

    /// <summary>Initializes a new collector.</summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="markers">The resolved marker symbols.</param>
    /// <param name="config">The configuration read from the trigger attribute.</param>
    /// <param name="diagnostics">The list that receives discovery diagnostics.</param>
    public Collector(Compilation compilation, Markers markers, MediatorConfig config, List<DiagnosticInfo> diagnostics)
    {
        _compilation = compilation;
        _markers = markers;
        _config = config;
        _diagnostics = diagnostics;
    }

    /// <summary>Inspects a candidate type and records any mediator handlers, behaviors or message types it declares.</summary>
    /// <param name="type">The candidate type to inspect.</param>
    public void Process(INamedTypeSymbol type)
    {
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            return;

        if (type.IsImplicitlyDeclared)
            return;

        bool accessible = _compilation.IsSymbolAccessibleWithin(type, _compilation.Assembly);
        bool instantiable = !type.IsAbstract && !type.IsStatic;

        foreach (var iface in type.AllInterfaces)
        {
            var def = iface.OriginalDefinition;

            if (s_Cmp.Equals(def, _markers.IRequest))
                AddRequestPair(_requestPairs, iface);
            else if (s_Cmp.Equals(def, _markers.IStreamRequest))
                AddRequestPair(_streamPairs, iface);
            else if (s_Cmp.Equals(iface, _markers.INotification))
                AddNotificationType(type);
            else if (s_Cmp.Equals(def, _markers.IRequestHandler))
                HandleRequestHandler(type, iface, accessible, instantiable, isStream: false);
            else if (s_Cmp.Equals(def, _markers.IStreamRequestHandler))
                HandleRequestHandler(type, iface, accessible, instantiable, isStream: true);
            else if (s_Cmp.Equals(def, _markers.INotificationHandler))
                HandleNotificationHandler(type, iface, accessible, instantiable);
            else if (s_Cmp.Equals(def, _markers.IPipelineBehavior))
                HandleBehavior(type, iface, accessible, instantiable, isStream: false);
            else if (s_Cmp.Equals(def, _markers.IStreamPipelineBehavior))
                HandleBehavior(type, iface, accessible, instantiable, isStream: true);
        }
    }

    private void AddRequestPair(Dictionary<string, (ITypeSymbol, ITypeSymbol)> target, INamedTypeSymbol iface)
    {
        if (iface.TypeArguments.Length != 2)
            return;

        var req = iface.TypeArguments[0];
        var resp = iface.TypeArguments[1];
        if (!req.IsFullyAccessible(_compilation) || !resp.IsFullyAccessible(_compilation))
            return;

        target[req.Fqn() + "|" + resp.Fqn()] = (req, resp);
    }

    private void AddNotificationType(INamedTypeSymbol type)
    {
        if (!type.IsFullyAccessible(_compilation))
            return;

        _notificationTypes[type.Fqn()] = type;
    }

    private void HandleRequestHandler(INamedTypeSymbol type, INamedTypeSymbol iface, bool accessible, bool instantiable, bool isStream)
    {
        if (isStream ? !_config.RegisterStreamRequestHandlers : !_config.RegisterRequestHandlers)
            return;

        // Open-generic request handlers are not a supported pattern (a request maps to one handler).
        if (type.IsOpenGeneric())
            return;

        if (!instantiable)
        {
            Report(Diagnostics.s_NonInstantiableHandler, type, type.Fqn());
            return;
        }

        if (!accessible)
        {
            Report(Diagnostics.s_InaccessibleHandler, type, type.Fqn());
            return;
        }

        var req = iface.TypeArguments[0];
        var resp = iface.TypeArguments[1];
        if (!req.IsFullyAccessible(_compilation))
        {
            Report(Diagnostics.s_InaccessibleTypeArgument, type, type.Fqn(), req.Fqn());
            return;
        }

        if (!resp.IsFullyAccessible(_compilation))
        {
            Report(Diagnostics.s_InaccessibleTypeArgument, type, type.Fqn(), resp.Fqn());
            return;
        }

        string handlerFqn = type.Fqn();
        string key = req.Fqn() + "|" + resp.Fqn();
        var map = isStream ? _streamHandlers : _requestHandlers;

        if (map.TryGetValue(key, out var existing))
        {
            if (existing.HandlerFqn != handlerFqn)
            {
                Report(
                    isStream ? Diagnostics.s_DuplicateStreamRequestHandler : Diagnostics.s_DuplicateRequestHandler,
                    type, existing.HandlerFqn, handlerFqn, req.Fqn());
            }

            return;
        }

        map[key] = (handlerFqn, type);
        (isStream ? _streamHandlerModels : _requestHandlerModels).Add((handlerFqn, req.Fqn(), resp.Fqn()));
    }

    private void HandleNotificationHandler(INamedTypeSymbol type, INamedTypeSymbol iface, bool accessible, bool instantiable)
    {
        if (!_config.RegisterNotificationHandlers)
            return;

        if (!instantiable)
        {
            Report(Diagnostics.s_NonInstantiableHandler, type, type.Fqn());
            return;
        }

        if (!accessible)
        {
            Report(Diagnostics.s_InaccessibleHandler, type, type.Fqn());
            return;
        }

        if (type.IsOpenGeneric())
        {
            if (IsStandardOpenShape(type, iface))
                _openNotificationHandlers.Add(type);

            return;
        }

        var notification = iface.TypeArguments[0];
        if (!notification.IsFullyAccessible(_compilation))
        {
            Report(Diagnostics.s_InaccessibleTypeArgument, type, type.Fqn(), notification.Fqn());
            return;
        }

        string handlerFqn = type.Fqn();
        string key = handlerFqn + "|" + notification.Fqn();
        if (_concreteNotificationKeys.Add(key))
            _concreteNotificationHandlers.Add((handlerFqn, notification.Fqn()));
    }

    private void HandleBehavior(INamedTypeSymbol type, INamedTypeSymbol iface, bool accessible, bool instantiable, bool isStream)
    {
        if (isStream ? !_config.RegisterStreamPipelineBehaviors : !_config.RegisterPipelineBehaviors)
            return;

        if (!instantiable)
        {
            Report(Diagnostics.s_NonInstantiableHandler, type, type.Fqn());
            return;
        }

        if (!accessible)
        {
            Report(Diagnostics.s_InaccessibleHandler, type, type.Fqn());
            return;
        }

        bool hasPriority = TryGetPriority(type, out int priority);

        if (type.IsOpenGeneric())
        {
            if (IsStandardOpenShape(type, iface))
                _openBehaviors.Add((type, isStream, priority, hasPriority));

            return;
        }

        var req = iface.TypeArguments[0];
        var resp = iface.TypeArguments[1];
        if (!req.IsFullyAccessible(_compilation))
        {
            Report(Diagnostics.s_InaccessibleTypeArgument, type, type.Fqn(), req.Fqn());
            return;
        }

        if (!resp.IsFullyAccessible(_compilation))
        {
            Report(Diagnostics.s_InaccessibleTypeArgument, type, type.Fqn(), resp.Fqn());
            return;
        }

        _concreteBehaviors.Add(new BehaviorModel(
            IsStream: isStream,
            IsOpenGeneric: false,
            HandlerFqn: type.Fqn(),
            OpenHandlerTypeOf: string.Empty,
            RequestFqn: req.Fqn(),
            ResponseFqn: resp.Fqn(),
            Priority: priority,
            HasPriority: hasPriority,
            ClosedInstances: EquatableArray<ClosedBehaviorInstance>.s_Empty));
    }

    /// <summary>Produces the final discovery model, resolving open generics into closed instantiations.</summary>
    /// <param name="hasMicrosoftDI">Whether the Microsoft.Extensions.DependencyInjection entry point should be emitted.</param>
    /// <param name="hasSnowberryDI">Whether the Snowberry.DependencyInjection entry point should be emitted.</param>
    /// <returns>The value-equatable discovery model.</returns>
    public DiscoveryModel Build(bool hasMicrosoftDI, bool hasSnowberryDI)
    {
        var requestHandlers = new List<RequestHandlerModel>();
        foreach (var (handler, req, resp) in _requestHandlerModels)
            requestHandlers.Add(new RequestHandlerModel(false, handler, req, resp));
        foreach (var (handler, req, resp) in _streamHandlerModels)
            requestHandlers.Add(new RequestHandlerModel(true, handler, req, resp));
        requestHandlers.Sort(static (a, b) =>
            string.CompareOrdinal(a.RequestFqn + a.HandlerFqn, b.RequestFqn + b.HandlerFqn));

        var behaviors = new List<BehaviorModel>(_concreteBehaviors);
        foreach (var open in _openBehaviors)
            behaviors.Add(BuildOpenBehavior(open.Type, open.IsStream, open.Priority, open.HasPriority));
        behaviors.Sort(static (a, b) => string.CompareOrdinal(SortKey(a), SortKey(b)));

        var notificationHandlers = new List<NotificationHandlerModel>();
        // Open-generic (flattened) handlers first so they run before concrete handlers within each notification group.
        foreach (var open in _openNotificationHandlers)
        {
            foreach (var notification in OrderedNotificationTypes())
            {
                if (!open.SatisfiesConstraints(ImmutableArray.Create(notification), _compilation))
                    continue;

                var closed = open.Construct(notification);
                notificationHandlers.Add(new NotificationHandlerModel(closed.Fqn(), notification.Fqn(), FromOpenGeneric: true));
            }
        }

        var concrete = new List<NotificationHandlerModel>();
        foreach (var (handler, notification) in _concreteNotificationHandlers)
            concrete.Add(new NotificationHandlerModel(handler, notification, FromOpenGeneric: false));
        concrete.Sort(static (a, b) => string.CompareOrdinal(a.NotificationFqn + a.HandlerFqn, b.NotificationFqn + b.HandlerFqn));
        notificationHandlers.AddRange(concrete);

        var model = new DiscoveryModel(
            EquatableArray<RequestHandlerModel>.From(requestHandlers),
            EquatableArray<BehaviorModel>.From(behaviors),
            EquatableArray<NotificationHandlerModel>.From(notificationHandlers),
            hasMicrosoftDI,
            hasSnowberryDI,
            EquatableArray<DiagnosticInfo>.From(_diagnostics));

        if (!model.HasAnyHandlers)
        {
            _diagnostics.Add(new DiagnosticInfo(Diagnostics.s_NoHandlersDiscovered.Id, EquatableArray<string>.s_Empty, null));

            // Recreate with the appended diagnostic.
            model = model with { Diagnostics = EquatableArray<DiagnosticInfo>.From(_diagnostics) };
        }

        return model;
    }

    private BehaviorModel BuildOpenBehavior(INamedTypeSymbol type, bool isStream, int priority, bool hasPriority)
    {
        var pairs = isStream ? _streamPairs : _requestPairs;
        var closed = new List<ClosedBehaviorInstance>();

        foreach (var pair in pairs.Values)
        {
            var args = ImmutableArray.Create(pair.Req, pair.Resp);
            if (!type.SatisfiesConstraints(args, _compilation))
                continue;

            var closedType = type.Construct(pair.Req, pair.Resp);
            closed.Add(new ClosedBehaviorInstance(closedType.Fqn(), pair.Req.Fqn(), pair.Resp.Fqn()));
        }

        closed.Sort(static (a, b) => string.CompareOrdinal(a.RequestFqn + a.ResponseFqn, b.RequestFqn + b.ResponseFqn));

        return new BehaviorModel(
            IsStream: isStream,
            IsOpenGeneric: true,
            HandlerFqn: type.OpenTypeOf(),
            OpenHandlerTypeOf: type.OpenTypeOf(),
            RequestFqn: string.Empty,
            ResponseFqn: string.Empty,
            Priority: priority,
            HasPriority: hasPriority,
            ClosedInstances: EquatableArray<ClosedBehaviorInstance>.From(closed));
    }

    private IEnumerable<ITypeSymbol> OrderedNotificationTypes()
    {
        var list = new List<ITypeSymbol>(_notificationTypes.Values);
        list.Sort(static (a, b) => string.CompareOrdinal(a.Fqn(), b.Fqn()));
        return list;
    }

    private bool TryGetPriority(INamedTypeSymbol type, out int priority)
    {
        priority = 0;
        foreach (var attribute in type.GetAttributes())
        {
            if (!s_Cmp.Equals(attribute.AttributeClass, _markers.PriorityAttribute))
                continue;

            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "Priority" && named.Value.Value is int value)
                    priority = value;
            }

            return true;
        }

        return false;
    }

    private static bool IsStandardOpenShape(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        if (iface.TypeArguments.Length != type.TypeParameters.Length)
            return false;

        for (int i = 0; i < iface.TypeArguments.Length; i++)
        {
            if (iface.TypeArguments[i] is not ITypeParameterSymbol tp || !s_Cmp.Equals(tp, type.TypeParameters[i]))
                return false;
        }

        return true;
    }

    private static string SortKey(BehaviorModel b) =>
        (b.IsStream ? "1" : "0") + b.HandlerFqn + b.RequestFqn + b.ResponseFqn;

    private void Report(DiagnosticDescriptor descriptor, ISymbol symbol, params string[] args)
    {
        _diagnostics.Add(new DiagnosticInfo(
            descriptor.Id,
            new EquatableArray<string>(args),
            LocationInfo.From(symbol)));
    }
}
